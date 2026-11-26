using System.Drawing.Drawing2D;
using System.Text.Json;

namespace Sector;

public sealed class SceneEditorForm : Form
{
    private static readonly Color Bg = Color.FromArgb(20,23,28), Surface = Color.FromArgb(29,34,41),
        Surface2 = Color.FromArgb(37,43,51), Orange = Color.FromArgb(235,133,43), White = Color.FromArgb(235,238,242),
        Muted = Color.FromArgb(151,161,174);
    private readonly List<SceneElement> _items = new();
    private readonly Panel _canvas = new() { BackColor = Color.FromArgb(12,14,18) };
    private readonly Panel _host = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(12,14,18) };
    private readonly ListBox _layers = new() { Dock = DockStyle.Fill, BackColor = Surface2, ForeColor = White, BorderStyle = BorderStyle.None };
    private readonly TextBox _name = new() { Text = "Новая сцена" };
    private readonly TextBox _text = new();
    private readonly TextBox _objectName = new();
    private readonly NumericUpDown _x = Num(0,3840), _y = Num(0,2160), _w = Num(8,3840,240), _h = Num(8,2160,60), _font = Num(6,180,28);
    private readonly ComboBox _resolution = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };
    private readonly Stack<string> _undo = new(), _redo = new();
    private readonly string _autosave = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Sector","Autosave","scene-0.4.json");
    private string _selected = "", _bg = "#101820", _color = "#F0A04B", _path = "";
    private float _scale = 1;
    private bool _loading, _drag, _resize, _dirty, _showGrid = true;
    private float _zoom = 1f;
    private PointF _offset;

    public SceneEditorForm()
    {
        Text = "Сектор — Scene Editor 0.4.0"; StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1450,900); MinimumSize = new Size(1100,700); BackColor = Bg; ForeColor = White;
        Font = new Font("Segoe UI",9f); KeyPreview = true;
        BuildUi(); RestoreOrCreate();
        _timer.Tick += (_,_) => Autosave();
        _timer.Start();
        KeyDown += Shortcuts;
        FormClosing += (_,_) => { Autosave(); _timer.Stop(); };
        _host.Resize += (_,_) => FitCanvas();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3, Padding=new Padding(10), BackColor=Bg };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,26));
        Controls.Add(root);
        var bar = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false, BackColor=Bg, Padding=new Padding(0,5,0,0) };
        AddButton(bar,"Новый",NewScene); AddButton(bar,"Открыть",OpenScene); AddButton(bar,"Сохранить",SaveScene,true);
        AddButton(bar,"+ Текст",AddText); AddButton(bar,"+ Изображение",AddImage); AddButton(bar,"Дублировать",DuplicateSelected); AddButton(bar,"По центру",CenterSelected); AddButton(bar,"Сетка",ToggleGrid); AddButton(bar,"−",()=>SetZoom(_zoom-0.1f)); AddButton(bar,"+",()=>SetZoom(_zoom+0.1f)); AddButton(bar,"Отменить",Undo); AddButton(bar,"Повторить",Redo);
        root.Controls.Add(bar,0,0);

        var body = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=3, RowCount=1, BackColor=Bg };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,250));
        root.Controls.Add(body,0,1);
        var left = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3, Padding=new Padding(8), BackColor=Surface };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute,30)); left.RowStyles.Add(new RowStyle(SizeType.Percent,100)); left.RowStyles.Add(new RowStyle(SizeType.Absolute,80));
        left.Controls.Add(Header("СЛОИ"),0,0); _layers.Font=new Font("Segoe UI",9.5f); _layers.SelectedIndexChanged+=(_,_)=>SelectLayer(); left.Controls.Add(_layers,0,1);
        var layerButtons=new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=true };
        AddButton(layerButtons,"Вверх",()=>MoveLayer(-1)); AddButton(layerButtons,"Вниз",()=>MoveLayer(1)); AddButton(layerButtons,"Удалить",DeleteSelected); AddButton(layerButtons,"Фон",ChooseBackground);
        left.Controls.Add(layerButtons,0,2); body.Controls.Add(left,0,0);

        var middle=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=2, Padding=new Padding(8,0,8,0), BackColor=Bg };
        middle.RowStyles.Add(new RowStyle(SizeType.Absolute,38)); middle.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var top=new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
        top.Controls.Add(Header("РАБОЧАЯ СЦЕНА")); _resolution.Items.AddRange(["800 × 600","1280 × 720","1366 × 768","1920 × 1080","Другое…"]);
        _resolution.SelectedIndexChanged+=(_,_)=>ResolutionChanged(); top.Controls.Add(_resolution);
        middle.Controls.Add(top,0,0); _canvas.Paint+=PaintCanvas; _canvas.MouseDown+=CanvasDown; _canvas.MouseMove+=CanvasMove;
        _canvas.MouseUp+=(_,_)=>{_drag=false;_resize=false;}; _canvas.Cursor=Cursors.Default; _host.Controls.Add(_canvas); middle.Controls.Add(_host,0,1); body.Controls.Add(middle,1,0);

        var right=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=2, Padding=new Padding(10), BackColor=Surface };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute,30)); right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        right.Controls.Add(Header("СВОЙСТВА"),0,0);
        var props=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=10, AutoScroll=true, BackColor=Surface };
        for(int i=0;i<10;i++) props.RowStyles.Add(new RowStyle(SizeType.Absolute,i==2?100:62));
        AddField(props,"Название сцены",_name,0); AddField(props,"Имя слоя",_objectName,1); AddField(props,"Текст объекта",_text,2,true);
        AddField(props,"X",_x,3); AddField(props,"Y",_y,4); AddField(props,"Ширина",_w,5); AddField(props,"Высота",_h,6); AddField(props,"Размер шрифта",_font,7);
        var colorPanel=new Panel { Dock=DockStyle.Fill, BackColor=Surface }; colorPanel.Controls.Add(new Label { Text="Цвет текста",Dock=DockStyle.Top,Height=22,ForeColor=Muted });
        var colorButton=Button("Выбрать цвет",ChooseColor); colorButton.Dock=DockStyle.Bottom; colorPanel.Controls.Add(colorButton); props.Controls.Add(colorPanel,0,8);
        var apply=Button("Применить свойства",ApplyProperties,true); apply.Dock=DockStyle.Fill; props.Controls.Add(apply,0,9);
        right.Controls.Add(props,0,1); body.Controls.Add(right,2,0);
        root.Controls.Add(_status,0,2);
        _resolution.SelectedIndex=1; _name.TextChanged += (_,_) => { if(!_loading) _dirty=true; };
    }

    private static NumericUpDown Num(decimal min,decimal max,decimal val=0)=>new(){Minimum=min,Maximum=max,Value=val,Dock=DockStyle.Bottom,BackColor=Surface2,ForeColor=White,BorderStyle=BorderStyle.FixedSingle};
    private static Label Header(string t)=>new(){Text=t,Dock=DockStyle.Fill,ForeColor=Muted,Font=new Font("Segoe UI Semibold",8.5f),TextAlign=ContentAlignment.MiddleLeft};
    private static void AddField(TableLayoutPanel p,string label,Control c,int row,bool multi=false)
    {
        var panel=new Panel{Dock=DockStyle.Fill,BackColor=Surface};
        panel.Controls.Add(new Label{Text=label,Dock=DockStyle.Top,Height=22,ForeColor=Muted});
        if(c is TextBox t){t.Dock=DockStyle.Fill;t.BackColor=Surface2;t.ForeColor=White;t.BorderStyle=BorderStyle.FixedSingle;if(multi){t.Multiline=true;t.ScrollBars=ScrollBars.Vertical;}}
        panel.Controls.Add(c);p.Controls.Add(panel,0,row);
    }
    private static void AddButton(FlowLayoutPanel p,string text,Action action,bool primary=false)=>p.Controls.Add(Button(text,action,primary));
    private static Button Button(string text,Action action,bool primary=false)
    {
        var b=new Button{Text=text,AutoSize=true,Height=31,FlatStyle=FlatStyle.Flat,BackColor=primary?Orange:Surface2,ForeColor=primary?Color.Black:White,Padding=new Padding(8,1,8,1),Margin=new Padding(0,0,5,0),Cursor=Cursors.Hand};
        b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>action();return b;
    }

    private void RestoreOrCreate()
    {
        try{if(File.Exists(_autosave)&&MessageBox.Show(this,"Найдено автосохранение. Восстановить сцену?","Сектор",MessageBoxButtons.YesNo)==DialogResult.Yes){LoadProjectState(SceneProjectService.Load(_autosave));SetStatus("Автосохранение восстановлено.");return;}}
        catch(Exception ex){SetStatus("Не удалось восстановить автосохранение: "+ex.Message);}
        CreateDefault();
    }
    private void CreateDefault()
    {
        _items.Clear();_items.Add(new SceneElement(Guid.NewGuid().ToString("N"),"Заголовок",SceneElementType.Text,80,80,560,100,"Новая сцена","Segoe UI",44,"#F0A04B"));
        _name.Text="Новая сцена";_bg="#101820";_selected=_items[0].Id;_undo.Clear();_redo.Clear();_dirty=false;RefreshLayers();SelectCurrent();FitCanvas();
    }
    private void AddText()
    {
        PushUndo();var e=new SceneElement(Guid.NewGuid().ToString("N"),"Текст "+(_items.Count+1),SceneElementType.Text,80,140,400,70,"Новый текст","Segoe UI",30,_color);
        _items.Add(e);_selected=e.Id;Changed();
    }
    private void AddImage()
    {
        using var d=new OpenFileDialog{Title="Добавить изображение",Filter="Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif"};
        if(d.ShowDialog(this)!=DialogResult.OK)return;
        try{using var img=Image.FromFile(d.FileName);if(img.Width>8192||img.Height>8192)throw new InvalidDataException("Изображение слишком большое.");
            using var ms=new MemoryStream();img.Save(ms,System.Drawing.Imaging.ImageFormat.Png);PushUndo();
            var e=new SceneElement(Guid.NewGuid().ToString("N"),Path.GetFileNameWithoutExtension(d.FileName),SceneElementType.Image,100,120,Math.Min(420,img.Width),Math.Min(320,img.Height),"","Segoe UI",16,"#FFFFFF",Convert.ToBase64String(ms.ToArray()));
            _items.Add(e);_selected=e.Id;Changed();
        }catch(Exception ex){Error("Изображение",ex);}
    }
    private void DeleteSelected(){var i=_items.FindIndex(e=>e.Id==_selected);if(i<0)return;PushUndo();_items.RemoveAt(i);_selected=_items.LastOrDefault()?.Id??"";Changed();}
    private void MoveLayer(int dir){var i=_items.FindIndex(e=>e.Id==_selected);var j=i+dir;if(i<0||j<0||j>=_items.Count)return;PushUndo();(_items[i],_items[j])=(_items[j],_items[i]);Changed();}
    private void SelectLayer(){if(_loading||_layers.SelectedIndex<0)return;_selected=_items[_items.Count-1-_layers.SelectedIndex].Id;SelectCurrent();}
    private void SelectCurrent()
    {
        var e=_items.FirstOrDefault(x=>x.Id==_selected);_loading=true;
        if(e!=null){_objectName.Text=e.Name;_text.Text=e.Text;_x.Value=Clamp(e.X,_x);_y.Value=Clamp(e.Y,_y);_w.Value=Clamp(e.Width,_w);_h.Value=Clamp(e.Height,_h);_font.Value=Clamp(e.FontSize,_font);_color=e.ColorHex;}
        else{_objectName.Clear();_text.Clear();_x.Value=0;_y.Value=0;_w.Value=8;_h.Value=8;_font.Value=28;}
        _loading=false;_canvas.Invalidate();
    }
    private static decimal Clamp(float v,NumericUpDown n)=>Math.Clamp((decimal)Math.Round(v),n.Minimum,n.Maximum);
    private void ApplyProperties()
    {
        var i=_items.FindIndex(e=>e.Id==_selected);if(i<0){SetStatus("Выберите объект на сцене или в слоях.");return;}
        PushUndo();var e=_items[i];var w=(float)_w.Value;var h=(float)_h.Value;
        _items[i]=e with{Name=string.IsNullOrWhiteSpace(_objectName.Text)?"Объект":_objectName.Text.Trim(),Text=_text.Text,X=Math.Clamp((float)_x.Value,0,_canvasWidth-w),Y=Math.Clamp((float)_y.Value,0,_canvasHeight-h),Width=w,Height=h,FontSize=(float)_font.Value,ColorHex=_color};
        Changed();
    }
    private int _canvasWidth=1280,_canvasHeight=720;
    private void ChooseColor(){using var d=new ColorDialog{Color=ColorTranslator.FromHtml(_color),FullOpen=true};if(d.ShowDialog(this)==DialogResult.OK){_color=ColorTranslator.ToHtml(d.Color);ApplyProperties();}}
    private void ChooseBackground(){using var d=new ColorDialog{Color=ColorTranslator.FromHtml(_bg),FullOpen=true};if(d.ShowDialog(this)==DialogResult.OK){PushUndo();_bg=ColorTranslator.ToHtml(d.Color);Changed();}}
    private void ResolutionChanged()
    {
        if(_loading)return;var size=_resolution.SelectedIndex switch{0=>(800,600),1=>(1280,720),2=>(1366,768),3=>(1920,1080),_=>(_canvasWidth,_canvasHeight)};
        if(_resolution.SelectedIndex==4){using var d=new ResolutionDialog(_canvasWidth,_canvasHeight);if(d.ShowDialog(this)!=DialogResult.OK)return;size=(d.CanvasWidth,d.CanvasHeight);}
        PushUndo();_canvasWidth=size.Item1;_canvasHeight=size.Item2;
        var adjusted=_items.Select(e=>e with{X=Math.Clamp(e.X,0,Math.Max(0,_canvasWidth-e.Width)),Y=Math.Clamp(e.Y,0,Math.Max(0,_canvasHeight-e.Height)),Width=Math.Min(e.Width,_canvasWidth),Height=Math.Min(e.Height,_canvasHeight)}).ToList(); _items.Clear(); _items.AddRange(adjusted);
        Changed();
    }
    private void FitCanvas()
    {
        if(_canvasWidth<1)return;var w=Math.Max(200,_host.ClientSize.Width-25);var h=Math.Max(200,_host.ClientSize.Height-25);
        _scale=Math.Clamp(Math.Min(w/(float)_canvasWidth,h/(float)_canvasHeight),.1f,1f);
        _canvas.Size=new Size((int)(_canvasWidth*_scale),(int)(_canvasHeight*_scale));_canvas.Invalidate();
    }
    private void PaintCanvas(object? s,PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(ColorTranslator.FromHtml(_bg));g.ScaleTransform(_scale,_scale);
        foreach(var item in _items)
        {
            var rect=new RectangleF(item.X,item.Y,item.Width,item.Height);
            if(item.Type==SceneElementType.Text){using var f=new Font(item.FontFamily,item.FontSize,FontStyle.Regular,GraphicsUnit.Pixel);using var b=new SolidBrush(ColorTranslator.FromHtml(item.ColorHex));g.DrawString(item.Text,f,b,rect);}
            else if(item.ImageBase64!=null){try{using var ms=new MemoryStream(Convert.FromBase64String(item.ImageBase64));using var im=Image.FromStream(ms);g.DrawImage(im,rect);}catch{}}
            if(item.Id==_selected){using var p=new Pen(Orange,2/_scale){DashStyle=DashStyle.Dash};g.DrawRectangle(p,item.X,item.Y,item.Width,item.Height);using var b=new SolidBrush(Orange);g.FillRectangle(b,item.X+item.Width-8/_scale,item.Y+item.Height-8/_scale,8/_scale,8/_scale);}
        }
    }
    private void CanvasDown(object? s,MouseEventArgs e)
    {
        var p=new PointF(e.X/_scale,e.Y/_scale);var selected=_items.FirstOrDefault(x=>x.Id==_selected);
        if(selected!=null&&p.X>=selected.X+selected.Width-14&&p.Y>=selected.Y+selected.Height-14&&p.X<=selected.X+selected.Width+2&&p.Y<=selected.Y+selected.Height+2){PushUndo();_resize=true;return;}
        var hit=_items.AsEnumerable().Reverse().FirstOrDefault(x=>p.X>=x.X&&p.X<=x.X+x.Width&&p.Y>=x.Y&&p.Y<=x.Y+x.Height);
        if(hit==null){_selected="";RefreshLayers();SelectCurrent();return;}
        _selected=hit.Id;RefreshLayers();SelectCurrent();PushUndo();_drag=true;_offset=new PointF(p.X-hit.X,p.Y-hit.Y);
    }
    private void CanvasMove(object? s,MouseEventArgs e)
    {
        var p=new PointF(e.X/_scale,e.Y/_scale);var i=_items.FindIndex(x=>x.Id==_selected);
        if(_drag&&i>=0){var a=_items[i];_items[i]=a with{X=Math.Clamp(p.X-_offset.X,0,_canvasWidth-a.Width),Y=Math.Clamp(p.Y-_offset.Y,0,_canvasHeight-a.Height)};_canvas.Invalidate();_dirty=true;}
        else if(_resize&&i>=0){var a=_items[i];_items[i]=a with{Width=Math.Clamp(p.X-a.X,8,_canvasWidth-a.X),Height=Math.Clamp(p.Y-a.Y,8,_canvasHeight-a.Y)};_canvas.Invalidate();_dirty=true;}
        else{var a=_items.FirstOrDefault(x=>x.Id==_selected);_canvas.Cursor=a!=null&&p.X>a.X+a.Width-14&&p.Y>a.Y+a.Height-14?Cursors.SizeNWSE:Cursors.SizeAll;}
    }
    private SceneProject Project()=>new(1,_name.Text,_canvasWidth,_canvasHeight,_bg,_items.ToList());
    private string Snapshot()=>JsonSerializer.Serialize(Project());
    private void PushUndo(){_undo.Push(Snapshot());if(_undo.Count>80){var a=_undo.Reverse().Take(80).ToArray();_undo.Clear();foreach(var s in a)_undo.Push(s);}_redo.Clear();}
    private void Undo(){if(_undo.Count==0)return;_redo.Push(Snapshot());LoadProjectState(JsonSerializer.Deserialize<SceneProject>(_undo.Pop())!);_dirty=true;}
    private void Redo(){if(_redo.Count==0)return;_undo.Push(Snapshot());LoadProjectState(JsonSerializer.Deserialize<SceneProject>(_redo.Pop())!);_dirty=true;}
    private void Changed(){RefreshLayers();SelectCurrent();FitCanvas();_canvas.Invalidate();_dirty=true;SetStatus("Изменения есть · автосохранение включено");}
    private void RefreshLayers(){_loading=true;var id=_selected;_layers.Items.Clear();for(int i=_items.Count-1;i>=0;i--)_layers.Items.Add((_items[i].Type==SceneElementType.Image?"▧ ":"T  ")+_items[i].Name);var index=_items.FindIndex(x=>x.Id==id);if(index>=0)_layers.SelectedIndex=_items.Count-1-index;_loading=false;}
    private void Autosave(){if(!_dirty)return;try{SceneProjectService.Save(Project(),_autosave);_dirty=false;SetStatus("Автосохранено "+DateTime.Now.ToString("HH:mm:ss"));}catch(Exception ex){SetStatus("Ошибка автосохранения: "+ex.Message);}}
    private void SaveScene(){using var d=new SaveFileDialog{Filter="Сцена Сектора (*.sector.scene.json)|*.sector.scene.json",FileName="Scene.sector.scene.json",AddExtension=false};if(d.ShowDialog(this)!=DialogResult.OK)return;try{SceneProjectService.Save(Project(),d.FileName);SceneProjectService.Save(Project(),_autosave);_path=d.FileName;_dirty=false;SetStatus("Сохранено: "+Path.GetFileName(_path));}catch(Exception ex){Error("Сохранение",ex);}}
    private void OpenScene(){using var d=new OpenFileDialog{Filter="Сцены Сектора (*.sector.scene.json)|*.sector.scene.json|Все файлы|*.*"};if(d.ShowDialog(this)!=DialogResult.OK)return;if(_dirty&&MessageBox.Show(this,"Отбросить несохранённые изменения?","Открытие",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{LoadProjectState(SceneProjectService.Load(d.FileName));_path=d.FileName;SceneProjectService.Save(Project(),_autosave);_dirty=false;SetStatus("Открыто: "+Path.GetFileName(_path));}catch(Exception ex){Error("Открытие",ex);}}
    private void LoadProjectState(SceneProject p){SceneProjectService.Validate(p);_loading=true;_name.Text=p.Name;_canvasWidth=p.CanvasWidth;_canvasHeight=p.CanvasHeight;_bg=p.BackgroundHex;_items.Clear();_items.AddRange(p.Elements);_selected=_items.LastOrDefault()?.Id??"";_resolution.SelectedIndex=(_canvasWidth,_canvasHeight) switch{(800,600)=>0,(1280,720)=>1,(1366,768)=>2,(1920,1080)=>3,_=>4};_loading=false;_undo.Clear();_redo.Clear();RefreshLayers();SelectCurrent();FitCanvas();_canvas.Invalidate();}
    private void NewScene(){if(_dirty&&MessageBox.Show(this,"Создать новую сцену без сохранения?","Новая сцена",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;CreateDefault();_dirty=true;}
    private void Shortcuts(object? s,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.S){SaveScene();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.Z){Undo();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.Y){Redo();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){DeleteSelected();e.SuppressKeyPress=true;}}
    private void SetStatus(string t)=>_status.Text=t;
    private void Error(string title,Exception ex)=>MessageBox.Show(this,ex.Message,title,MessageBoxButtons.OK,MessageBoxIcon.Warning);
}

internal sealed class ResolutionDialog:Form
{
    private readonly NumericUpDown _width=new(){Minimum=320,Maximum=3840,Width=110};
    private readonly NumericUpDown _height=new(){Minimum=240,Maximum=2160,Width=110};
    public int CanvasWidth=>(int)_width.Value;public int CanvasHeight=>(int)_height.Value;
    public ResolutionDialog(int w,int h){Text="Размер сцены";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=false;MaximizeBox=false;ClientSize=new Size(270,170);_width.Value=Math.Clamp(w,320,3840);_height.Value=Math.Clamp(h,240,2160);var p=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown,WrapContents=false};p.Controls.Add(new Label{Text="Ширина",AutoSize=true});p.Controls.Add(_width);p.Controls.Add(new Label{Text="Высота",AutoSize=true});p.Controls.Add(_height);var ok=new Button{Text="Применить",DialogResult=DialogResult.OK};p.Controls.Add(ok);Controls.Add(p);AcceptButton=ok;}
}

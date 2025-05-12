using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// KnowledgeBaseForm.xaml 的交互逻辑
    /// </summary>
    public partial class KnowledgeBaseForm : Window
    {
        private Dictionary<string, FrameworkElement> _sectionElements;
        private List<FrameworkElement> _allSections;
        private StackPanel _mainContentPanel;
        private List<PlanetInfo> _allPlanets;
        private List<StarInfo> _allStars;
        private List<GalaxyInfo> _allGalaxies;

        public KnowledgeBaseForm()
        {
            try
            {
                InitializeComponent();
                
                // 初始化分类列表
                CategoryList.ItemsSource = new[] {
                    "太阳系",
                    "恒星",
                    "星系"
                };

                // 初始化数据
                InitializeData();

                // 设置数据绑定
                PlanetsList.ItemsSource = _allPlanets;
                StarsList.ItemsSource = _allStars;
                GalaxiesList.ItemsSource = _allGalaxies;

                // 注册加载完成事件
                Loaded += KnowledgeBaseForm_Loaded;

                // 注册搜索框文本变化事件
                SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeData()
        {
            _allPlanets = new List<PlanetInfo>
            {
                new PlanetInfo { 
                    Name = "水星", 
                    Description = "太阳系最内侧的行星，表面布满陨石坑，没有大气层保护。",
                    DetailInfo = "水星是太阳系中最小的行星，也是距离太阳最近的行星。它的表面布满了陨石坑，没有大气层的保护，因此温差极大，白天温度可达427℃，夜晚则降至-173℃。水星的自转周期为58.6个地球日，公转周期为88个地球日。"
                },
                new PlanetInfo { 
                    Name = "金星", 
                    Description = "太阳系最热的行星，表面温度可达462℃，有浓厚的二氧化碳大气层。",
                    DetailInfo = "金星是太阳系中第二颗行星，也是太阳系中最热的行星。它的大气层主要由二氧化碳组成，造成了强烈的温室效应，使表面温度高达462℃。金星的自转方向与公转方向相反，是太阳系中唯一一个逆向自转的行星。"
                },
                new PlanetInfo { 
                    Name = "地球", 
                    Description = "太阳系中唯一已知存在生命的行星，表面71%被水覆盖。",
                    DetailInfo = "地球是太阳系中第三颗行星，也是目前已知唯一存在生命的行星。地球表面71%被水覆盖，大气层主要由氮气和氧气组成。地球的自转周期为24小时，公转周期为365.25天。地球有一个天然卫星——月球。"
                },
                new PlanetInfo { 
                    Name = "火星", 
                    Description = "被称为红色星球，拥有太阳系最高的火山和最深的峡谷。",
                    DetailInfo = "火星是太阳系中第四颗行星，因其表面富含氧化铁而呈现红色。火星拥有太阳系最高的火山奥林匹斯山（高度约21.9公里）和最深的峡谷水手谷（长度约4000公里）。火星有两个小卫星：火卫一和火卫二。"
                },
                new PlanetInfo { 
                    Name = "木星", 
                    Description = "太阳系最大的行星，主要由氢和氦组成，有著名的大红斑。",
                    DetailInfo = "木星是太阳系中最大的行星，主要由氢和氦组成。它最著名的特征是大红斑，这是一个持续了至少300年的巨大风暴。木星有79颗已知的卫星，其中最大的四颗被称为伽利略卫星。"
                },
                new PlanetInfo { 
                    Name = "土星", 
                    Description = "以其壮观的环系而闻名，密度是太阳系行星中最低的。",
                    DetailInfo = "土星是太阳系中第二大的行星，以其壮观的环系而闻名。土星的密度是太阳系行星中最低的，如果有一个足够大的水池，土星会漂浮在水面上。土星有82颗已知的卫星，其中最大的土卫六是太阳系中唯一拥有浓厚大气层的卫星。"
                },
                new PlanetInfo { 
                    Name = "天王星", 
                    Description = "第一个通过望远镜发现的行星，自转轴几乎平行于公转轨道。",
                    DetailInfo = "天王星是太阳系中第七颗行星，是第一个通过望远镜发现的行星。它的自转轴几乎平行于公转轨道，就像是在侧躺着公转。天王星有27颗已知的卫星，其中最大的五颗是：天卫一、天卫二、天卫三、天卫四和天卫五。"
                },
                new PlanetInfo { 
                    Name = "海王星", 
                    Description = "太阳系最外侧的行星，有强烈的风暴系统，风速可达每小时2100公里。",
                    DetailInfo = "海王星是太阳系中最外侧的行星，有强烈的风暴系统，风速可达每小时2100公里。海王星有14颗已知的卫星，其中最大的海卫一是一个冰质天体，表面有活跃的地质活动。海王星的大气层主要由氢、氦和甲烷组成。"
                },
                new PlanetInfo {
                    Name = "冥王星",
                    Description = "曾经的第九大行星，现在被归为矮行星。",
                    DetailInfo = "冥王星曾被认为是太阳系的第九颗行星，但在2006年被国际天文学联合会重新归类为'矮行星'。它位于柯伊伯带，拥有稀薄的大气层，主要由氮、甲烷和一氧化碳组成。冥王星有五颗已知的卫星，其中最大的是卡戎。"
                },
                new PlanetInfo {
                    Name = "谷神星",
                    Description = "位于小行星带的矮行星，是小行星带中最大的天体。",
                    DetailInfo = "谷神星是太阳系中首个被发现的矮行星，同时也是小行星带中最大天体。它的直径约为940公里，表面可能存在地下水或冰，是研究太阳系早期历史的重要对象。"
                },
                new PlanetInfo {
                    Name = "小行星带",
                    Description = "火星和木星之间的天体带，包含大量小行星。",
                    DetailInfo = "小行星带位于火星和木星之间，是由数十万颗小行星组成的区域。它们是太阳系早期形成时遗留下来的物质。尽管数量众多，它们之间的间距非常大，航天器可以轻松穿过这个区域。"
                }
            };

            _allStars = new List<StarInfo>
            {
                new StarInfo { 
                    Name = "主序星", 
                    Description = "处于主序阶段的恒星，通过氢核聚变产生能量，如太阳。",
                    DetailInfo = "主序星是恒星演化过程中最稳定的阶段，通过氢核聚变产生能量。太阳就是一颗典型的主序星，已经在这个阶段存在了约46亿年，预计还将继续存在约50亿年。主序星的质量决定了它的寿命和最终演化方向。"
                },
                new StarInfo { 
                    Name = "红巨星", 
                    Description = "恒星演化后期的阶段，体积膨胀，表面温度降低。",
                    DetailInfo = "红巨星是恒星演化后期的阶段，当恒星核心的氢燃料耗尽后，外层会膨胀，表面温度降低，呈现红色。红巨星的体积可以膨胀到原来的数百倍，但密度很低。太阳在约50亿年后也会变成红巨星。"
                },
                new StarInfo { 
                    Name = "白矮星", 
                    Description = "恒星演化的最终阶段之一，密度极高，体积小。",
                    DetailInfo = "白矮星是中小质量恒星演化的最终阶段，密度极高，体积小。一茶匙白矮星物质的质量可达数吨。白矮星不再进行核聚变，只是缓慢冷却。太阳最终也会变成白矮星。"
                },
                new StarInfo { 
                    Name = "中子星", 
                    Description = "超新星爆发后的致密天体，由中子组成，密度极大。",
                    DetailInfo = "中子星是大质量恒星超新星爆发后的致密残骸，主要由中子组成。中子星的密度极高，一茶匙中子星物质的质量可达数亿吨。中子星的自转速度极快，有些甚至可以达到每秒数百转。"
                },
                new StarInfo { 
                    Name = "黑洞", 
                    Description = "引力极强的天体，连光都无法逃脱其引力场。",
                    DetailInfo = "黑洞是时空中的一个区域，其引力如此之强，以至于任何物质和辐射都无法逃脱。黑洞的形成通常与超新星爆发或大质量恒星的直接坍缩有关。黑洞的大小可以用史瓦西半径来描述，这个半径内的任何物质都无法逃脱。"
                },
                new StarInfo {
                    Name = "蓝巨星",
                    Description = "高温、高亮度的巨大恒星，寿命极短。",
                    DetailInfo = "蓝巨星是质量极大的恒星，表面温度可达2万K以上，颜色呈蓝白色。由于能量释放极快，它们的寿命通常只有几百万年。蓝巨星常常在生命尽头发生超新星爆炸，成为中子星或黑洞。"
                },
                new StarInfo {
                    Name = "棕矮星",
                    Description = "质量不足以维持氢聚变的失败恒星。",
                    DetailInfo = "棕矮星介于行星与恒星之间，质量不足以维持持续的氢聚变反应。它们发出的光非常微弱，主要靠重力坍缩产生能量。因为过于暗淡，棕矮星通常难以被发现。"
                },
                new StarInfo {
                    Name = "超新星",
                    Description = "恒星生命终结时的巨大爆炸事件。",
                    DetailInfo = "超新星是大质量恒星在核心崩塌后发生的剧烈爆炸，释放出极大的能量。它可以在短时间内亮过整个星系，并产生大量重元素。超新星也为中子星和黑洞的诞生铺平了道路。"
                }
            };

            _allGalaxies = new List<GalaxyInfo>
            {
                new GalaxyInfo { 
                    Name = "螺旋星系", 
                    Description = "具有旋臂结构的星系，如银河系，包含大量年轻恒星。",
                    DetailInfo = "螺旋星系是宇宙中最常见的星系类型之一，具有明显的旋臂结构。我们的银河系就是一个典型的螺旋星系。螺旋星系通常包含大量年轻恒星和星际物质，旋臂是恒星形成的主要区域。"
                },
                new GalaxyInfo { 
                    Name = "椭圆星系", 
                    Description = "呈椭圆形状的星系，主要由老年恒星组成，缺乏气体和尘埃。",
                    DetailInfo = "椭圆星系是宇宙中另一种常见的星系类型，呈椭圆形状。它们主要由老年恒星组成，缺乏气体和尘埃，因此很少形成新的恒星。椭圆星系的大小差异很大，从矮椭圆星系到超巨椭圆星系都有。"
                },
                new GalaxyInfo { 
                    Name = "不规则星系", 
                    Description = "没有明显对称结构的星系，通常富含气体和尘埃。",
                    DetailInfo = "不规则星系没有明显的对称结构，形状不规则。它们通常富含气体和尘埃，有活跃的恒星形成活动。不规则星系可能是由于与其他星系的相互作用或碰撞而形成的。"
                },
                new GalaxyInfo { 
                    Name = "棒旋星系", 
                    Description = "中心有棒状结构的螺旋星系，如银河系。",
                    DetailInfo = "棒旋星系是一种特殊的螺旋星系，其中心有一个明显的棒状结构。我们的银河系就是一个棒旋星系。棒状结构可能有助于将气体和尘埃输送到星系中心，促进恒星形成。"
                },
                new GalaxyInfo {
                    Name = "透镜星系",
                    Description = "介于螺旋星系和椭圆星系之间的过渡类型。",
                    DetailInfo = "透镜星系在形态上介于螺旋星系与椭圆星系之间，通常拥有圆盘结构但没有明显的旋臂。它们的恒星主要为老年恒星，缺乏恒星形成活动。"
                },
                new GalaxyInfo {
                    Name = "矮星系",
                    Description = "宇宙中最常见的星系类型，体积和质量较小。",
                    DetailInfo = "矮星系是体积和质量都相对较小的星系，虽然不引人注目，但却是宇宙中最常见的星系类型。它们通常围绕大型星系运转，例如银河系周围就有多个矮星系伴随。"
                },
                new GalaxyInfo {
                    Name = "仙女座星系",
                    Description = "距离银河系最近的大星系，正朝银河系靠近。",
                    DetailInfo = "仙女座星系是距离我们最近的大型星系，距离地球约250万光年。它与银河系极为相似，被认为未来会与银河系发生碰撞并合并。仙女座星系包含上千亿颗恒星。"
                }
            };
        }

        private void KnowledgeBaseForm_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 初始化各个部分的引用
                _sectionElements = new Dictionary<string, FrameworkElement>();
                _allSections = new List<FrameworkElement>();
                _mainContentPanel = MainScrollViewer.Content as StackPanel;
                
                if (_mainContentPanel != null)
                {
                    foreach (var child in _mainContentPanel.Children)
                    {
                        if (child is Border border)
                        {
                            if (border.Child is StackPanel titleBlock)
                            {
                                if (titleBlock.Children.Count > 0 && titleBlock.Children[0] is TextBlock textBlock)
                                {
                                    _sectionElements[textBlock.Text] = border;
                                    _allSections.Add(border);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载内容错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string searchText = SearchTextBox.Text.Trim().ToLower();
                if (_mainContentPanel == null) return;

                // 清空当前内容
                _mainContentPanel.Children.Clear();

                if (string.IsNullOrEmpty(searchText))
                {
                    // 如果搜索框为空，显示所有内容
                    foreach (var section in _allSections)
                    {
                        _mainContentPanel.Children.Add(section);
                    }
                    return;
                }

                // 创建新的内容面板
                var newContent = new StackPanel();

                // 搜索太阳系内容
                var matchingPlanets = _allPlanets.Where(p => 
                    p.Name.ToLower().Contains(searchText) || 
                    p.Description.ToLower().Contains(searchText)).ToList();

                if (matchingPlanets.Any())
                {
                    var solarSystemSection = CreateSection("太阳系", "太阳系是一个以太阳为中心的恒星系统，包含8颗行星、5颗矮行星以及众多小行星、彗星等天体。", matchingPlanets);
                    newContent.Children.Add(solarSystemSection);
                }

                // 搜索恒星内容
                var matchingStars = _allStars.Where(s => 
                    s.Name.ToLower().Contains(searchText) || 
                    s.Description.ToLower().Contains(searchText)).ToList();

                if (matchingStars.Any())
                {
                    var starsSection = CreateSection("恒星", "恒星是由引力束缚在一起的气体球，通过核聚变产生能量。它们在宇宙中扮演着重要角色，是星系的基本组成单位。", matchingStars);
                    newContent.Children.Add(starsSection);
                }

                // 搜索星系内容
                var matchingGalaxies = _allGalaxies.Where(g => 
                    g.Name.ToLower().Contains(searchText) || 
                    g.Description.ToLower().Contains(searchText)).ToList();

                if (matchingGalaxies.Any())
                {
                    var galaxiesSection = CreateSection("星系", "星系是由恒星、气体、尘埃和暗物质组成的巨大天体系统，在引力作用下聚集在一起。", matchingGalaxies);
                    newContent.Children.Add(galaxiesSection);
                }

                // 如果没有找到匹配项，显示提示
                if (newContent.Children.Count == 0)
                {
                    var noResultText = new TextBlock
                    {
                        Text = "未找到相关内容",
                        Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 255)),
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    };
                    newContent.Children.Add(noResultText);
                }

                // 更新主内容面板
                _mainContentPanel.Children.Add(newContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"搜索错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateSection<T>(string title, string description, List<T> items) where T : class
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 58)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel();

            // 标题
            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(Color.FromRgb(65, 105, 225)),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(titleBlock);

            // 描述
            var descriptionBlock = new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 255)),
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(descriptionBlock);

            // 子标题
            var subtitleBlock = new TextBlock
            {
                Text = title == "太阳系" ? "主要行星" : title == "恒星" ? "恒星类型" : "星系类型",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 208)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 10)
            };
            stackPanel.Children.Add(subtitleBlock);

            // 项目列表
            var itemsControl = new ItemsControl
            {
                ItemsSource = items
            };

            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.NameProperty, "itemBorder");
            factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(37, 37, 80)));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            factory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 10));
            factory.SetValue(Border.PaddingProperty, new Thickness(15));

            var itemGrid = new FrameworkElementFactory(typeof(Grid));
            var column1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            column1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var column2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            column2.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            itemGrid.AppendChild(column1);
            itemGrid.AppendChild(column2);

            var contentStack = new FrameworkElementFactory(typeof(StackPanel));
            contentStack.SetValue(Grid.ColumnProperty, 0);
            
            var nameBlock = new FrameworkElementFactory(typeof(TextBlock));
            nameBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            nameBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(65, 105, 225)));
            nameBlock.SetValue(TextBlock.FontSizeProperty, 18.0);
            nameBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);

            var descBlock = new FrameworkElementFactory(typeof(TextBlock));
            descBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Description"));
            descBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(224, 224, 255)));
            descBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            descBlock.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 0, 0));

            contentStack.AppendChild(nameBlock);
            contentStack.AppendChild(descBlock);

            // 添加查看详情按钮
            var button = new FrameworkElementFactory(typeof(Button));
            button.SetValue(Button.NameProperty, "detailButton");
            button.SetValue(Button.ContentProperty, "查看详情");
            button.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            button.SetValue(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(224, 224, 255)));
            button.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            button.SetValue(Button.PaddingProperty, new Thickness(20, 12, 20, 12));
            button.SetValue(Button.CursorProperty, Cursors.Hand);
            button.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            button.SetValue(Button.FontSizeProperty, 16.0);
            button.SetValue(Button.TemplateProperty, CreateButtonTemplate());
            button.SetValue(Grid.ColumnProperty, 1);
            button.SetValue(Button.MarginProperty, new Thickness(15, 0, 0, 0));
            button.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Center);

            // 添加按钮点击事件
            button.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is Button btn && btn.DataContext is T item)
                {
                    ShowDetailWindow(item);
                }
            }));

            itemGrid.AppendChild(contentStack);
            itemGrid.AppendChild(button);
            factory.AppendChild(itemGrid);

            template.VisualTree = factory;
            itemsControl.ItemTemplate = template;

            stackPanel.Children.Add(itemsControl);
            border.Child = stackPanel;

            return border;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(Button.ContentProperty));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // 添加触发器
            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            var setter = new Setter { Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromRgb(37, 37, 80)) };
            trigger.Setters.Add(setter);
            template.Triggers.Add(trigger);

            return template;
        }

        private void ShowDetailWindow<T>(T item) where T : class
        {
            try
            {
                string title = "";
                string detailInfo = "";

                if (item is PlanetInfo planet)
                {
                    title = planet.Name;
                    detailInfo = planet.DetailInfo;
                }
                else if (item is StarInfo star)
                {
                    title = star.Name;
                    detailInfo = star.DetailInfo;
                }
                else if (item is GalaxyInfo galaxy)
                {
                    title = galaxy.Name;
                    detailInfo = galaxy.DetailInfo;
                }

                var detailWindow = new Window
                {
                    Title = title,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush(Color.FromRgb(15, 15, 30))
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // 标题栏
                var titleBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(15, 15, 30))
                };
                titleBar.MouseLeftButtonDown += (s, e) => detailWindow.DragMove();

                var titleBarGrid = new Grid();
                titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = title,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 255)),
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(20, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var closeButton = new Button
                {
                    Content = "×",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 255)),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand
                };
                closeButton.Click += (s, e) => detailWindow.Close();

                Grid.SetColumn(titleText, 0);
                Grid.SetColumn(closeButton, 1);
                titleBarGrid.Children.Add(titleText);
                titleBarGrid.Children.Add(closeButton);
                titleBar.Child = titleBarGrid;

                // 内容区
                var contentBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 58)),
                    Margin = new Thickness(20),
                    CornerRadius = new CornerRadius(10)
                };

                var contentStack = new StackPanel
                {
                    Margin = new Thickness(20)
                };

                var detailText = new TextBlock
                {
                    Text = detailInfo,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 255)),
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 28
                };

                contentStack.Children.Add(detailText);
                contentBorder.Child = contentStack;

                Grid.SetRow(titleBar, 0);
                Grid.SetRow(contentBorder, 1);
                grid.Children.Add(titleBar);
                grid.Children.Add(contentBorder);

                detailWindow.Content = grid;
                detailWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示详情错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗口拖动错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // 触发搜索框文本变化事件
            SearchTextBox_TextChanged(sender, null);
        }

        private void AllCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CategoryList.SelectedItem = null;
                SearchTextBox.Clear();
                MainScrollViewer.ScrollToTop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"滚动错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 用 AddedItems 获取本次选中的项
                if (e.AddedItems == null || e.AddedItems.Count == 0) return;

                string selectedCategory = e.AddedItems[0]?.ToString();
                if (string.IsNullOrEmpty(selectedCategory)) return;

                FrameworkElement targetSection = null;

                if (_sectionElements.ContainsKey(selectedCategory))
                {
                    targetSection = _sectionElements[selectedCategory];
                }

                if (targetSection != null)
                {
                    ScrollToElement(targetSection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换分类时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScrollToElement(FrameworkElement element)
        {
            try
            {
                if (element == null || MainScrollViewer == null) return;

                // 确保元素已经加载
                element.UpdateLayout();

                // 计算元素相对于滚动视图的位置
                Point elementPosition = element.TransformToAncestor(MainScrollViewer)
                    .Transform(new Point(0, 0));

                // 滚动到元素位置
                MainScrollViewer.ScrollToVerticalOffset(elementPosition.Y);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"滚动到元素时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    var item = button.DataContext;
                    if (item is PlanetInfo planet)
                    {
                        ShowDetailWindow(planet);
                    }
                    else if (item is StarInfo star)
                    {
                        ShowDetailWindow(star);
                    }
                    else if (item is GalaxyInfo galaxy)
                    {
                        ShowDetailWindow(galaxy);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示详情错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 行星信息类
    public class PlanetInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailInfo { get; set; } = string.Empty;
    }

    // 恒星信息类
    public class StarInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailInfo { get; set; } = string.Empty;
    }

    // 星系信息类
    public class GalaxyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailInfo { get; set; } = string.Empty;
    }
} 
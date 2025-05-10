using System;
using System.Collections.Generic;
using System.Linq;
using HangKong_StarTrail.Models;

namespace HangKong_StarTrail.Services
{
    public class KnowledgeService : IKnowledgeService
    {
        private readonly List<KnowledgeItem> _items;

        public KnowledgeService()
        {
            var now = DateTime.Now;
            _items = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "1",
                    Title = "恒星的生命周期",
                    Summary = "恒星从诞生到死亡经历的不同阶段和演化过程",
                    Content = "恒星的生命周期包括恒星诞生、主序阶段、红巨星阶段和恒星死亡四个主要阶段。不同质量的恒星有不同的演化路径和寿命，质量越大的恒星寿命越短。",
                    Category = "恒星",
                    Tags = new List<string> { "恒星", "天体演化", "天文学基础" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "2",
                    Title = "银河系的结构",
                    Summary = "我们所在星系的基本结构和主要组成部分",
                    Content = "银河系是一个包含数千亿颗恒星的巨大星系，直径约10万光年。它的主要结构包括银心、银盘、旋臂、银晕和暗物质晕。",
                    Category = "星系",
                    Tags = new List<string> { "银河系", "星系结构", "宇宙尺度" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "3",
                    Title = "黑洞的形成与特性",
                    Summary = "宇宙中最神秘天体的形成机制和基本特性",
                    Content = "黑洞是时空中引力极强的区域，连光也无法逃脱。黑洞的关键特性和形成机制包括恒星坍缩、奇点、事件视界等概念。",
                    Category = "宇宙现象",
                    Tags = new List<string> { "黑洞", "广义相对论", "时空" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "4",
                    Title = "系外行星探测方法",
                    Summary = "探测太阳系外行星的主要技术和重要发现",
                    Content = "系外行星的探测方法包括凌星法、径向速度法、直接成像、引力微透镜法和天体测量法等。科学家已发现超过5000颗系外行星。",
                    Category = "行星",
                    Tags = new List<string> { "系外行星", "天文观测", "宜居带" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "5",
                    Title = "太阳系的形成",
                    Summary = "我们的行星系统如何从星际尘埃中形成",
                    Content = "太阳系形成于约46亿年前，经历了前太阳星云坍缩、原行星盘形成、太阳形成、行星形成和小天体形成等阶段。",
                    Category = "太阳系",
                    Tags = new List<string> { "太阳系", "行星形成", "天体演化" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "6",
                    Title = "中子星的特性",
                    Summary = "超高密度恒星残骸的奇特物理特性",
                    Content = "中子星是大质量恒星超新星爆发后的残骸，直径仅20公里左右，但质量可达太阳的1.4倍以上。它们极度致密，主要由中子组成，表面引力极强。中子星通常具有极强的磁场和极快的自转速度，形成脉冲星现象。",
                    Category = "恒星",
                    Tags = new List<string> { "中子星", "脉冲星", "致密天体" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "7",
                    Title = "太阳活动周期",
                    Summary = "太阳表面活动的周期性变化及其影响",
                    Content = "太阳活动呈现约11年的周期性变化，主要表现为太阳黑子数量的增减。活动高峰期太阳表面出现大量黑子、耀斑和日冕物质抛射，可能影响地球磁场、无线电通信并产生极光现象。太阳活动与太阳内部磁场翻转有关。",
                    Category = "恒星",
                    Tags = new List<string> { "太阳", "太阳黑子", "太阳风暴" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "8",
                    Title = "木星大红斑",
                    Summary = "太阳系最大行星上的持久风暴系统",
                    Content = "木星大红斑是太阳系中最大的风暴系统，至少已持续了400年。这个反气旋风暴长度约为地球直径的1.3倍，宽度约为地球直径的0.7倍。它呈现的红色可能来自于大气中的磷、硫等化合物。风暴边缘风速可达每小时640公里。",
                    Category = "行星",
                    Tags = new List<string> { "木星", "气态巨行星", "行星大气" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "9",
                    Title = "火星的水文历史",
                    Summary = "红色星球上曾经存在的液态水环境",
                    Content = "尽管现在火星表面极度干燥，但多项证据表明火星曾拥有液态水。火星表面的河床、峡谷和三角洲结构，以及矿物组成分析都支持这一结论。科学家推测火星在约30-35亿年前可能拥有海洋，覆盖了北半球近三分之一的表面。",
                    Category = "行星",
                    Tags = new List<string> { "火星", "液态水", "宜居性" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "10",
                    Title = "土卫六：泰坦",
                    Summary = "太阳系中唯一拥有浓密大气的卫星",
                    Content = "泰坦是土星最大的卫星，也是太阳系中唯一拥有浓密大气的卫星。它的大气主要由氮气组成，还有少量甲烷。表面温度约-179°C，在这种条件下甲烷可以形成液态，构成湖泊和海洋。泰坦表面有与地球相似的地质特征，如沙丘、山脉和河流系统。",
                    Category = "卫星",
                    Tags = new List<string> { "卫星", "土星", "液态甲烷" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "11",
                    Title = "伽马射线暴",
                    Summary = "宇宙中最剧烈的爆发现象",
                    Content = "伽马射线暴是宇宙中能量最强的爆发现象，在短短几秒到几分钟内释放的能量相当于太阳一生释放能量的总和。短时伽马射线暴（小于2秒）可能来自中子星合并，长时暴（大于2秒）则可能源于大质量恒星坍缩。这些爆发通常发生在遥远的星系中。",
                    Category = "宇宙现象",
                    Tags = new List<string> { "伽马射线", "高能天文学", "恒星死亡" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "12",
                    Title = "宇宙微波背景辐射",
                    Summary = "宇宙大爆炸的残余热辐射",
                    Content = "宇宙微波背景辐射是宇宙大爆炸后约38万年时产生的热辐射残余，现在已冷却至约2.7K（-270.45°C）。它几乎均匀地来自所有方向，被视为宇宙大爆炸理论的最强有力证据。其中的微小温度波动反映了宇宙早期物质分布的细微不均匀性，这些不均匀性最终演化成了现今的星系和星系团。",
                    Category = "宇宙现象",
                    Tags = new List<string> { "宇宙学", "大爆炸", "宇宙起源" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "13",
                    Title = "星系分类",
                    Summary = "哈勃序列与现代星系分类系统",
                    Content = "哈勃在1926年提出的星系分类系统将星系分为椭圆星系(E)、旋涡星系(S)和不规则星系(Irr)。旋涡星系又分为棒旋星系(SB)和普通旋涡星系。现代分类还包括透镜状星系(S0)和矮星系等类型。银河系是一个棒旋星系(SB)，而仙女座星系是一个普通旋涡星系(S)。",
                    Category = "星系",
                    Tags = new List<string> { "星系分类", "哈勃序列", "星系形态学" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "14",
                    Title = "活动星系核",
                    Summary = "星系中心的超大质量黑洞活动现象",
                    Content = "活动星系核是指星系中心区域异常明亮的现象，由中心超大质量黑洞吸积物质时释放的巨大能量引起。活动星系核包括类星体、布雷沙特星系、射电星系等多种类型。这些天体通常辐射强烈的X射线、射电波和伽马射线，有些还有从中心延伸出数百万光年的物质喷流。",
                    Category = "星系",
                    Tags = new List<string> { "AGN", "类星体", "超大质量黑洞" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "15",
                    Title = "哥白尼革命",
                    Summary = "从地心说到日心说的科学革命",
                    Content = "哥白尼革命是16世纪由尼古拉·哥白尼发起的科学革命，提出日心说取代地心说，认为行星(包括地球)围绕太阳运行。这一理论在当时极具颠覆性，遭到教会强烈反对。后来开普勒、伽利略和牛顿的工作进一步完善了这一模型，彻底改变了人类对宇宙的认知，标志着现代天文学的开端。",
                    Category = "天文学史",
                    Tags = new List<string> { "哥白尼", "日心说", "科学革命" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "16",
                    Title = "中国古代天文学",
                    Summary = "中国古代天文观测与历法发展",
                    Content = "中国古代天文学有着悠久历史，早在商代就有天象记录。汉代张衡发明地动仪，唐代僧一行测量子午线长度，宋代制作了授时历。中国古代对超新星、彗星有详细记录，其中1054年的超新星记录对应现在的蟹状星云。中国二十八宿的划分是世界上最早的星座系统之一。",
                    Category = "天文学史",
                    Tags = new List<string> { "中国天文", "古代观测", "二十八宿" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "17",
                    Title = "詹姆斯·韦布太空望远镜",
                    Summary = "下一代红外太空天文台的性能与科学目标",
                    Content = "詹姆斯·韦布太空望远镜是哈勃太空望远镜的后继者，主要在红外波段观测。其主镜直径6.5米，由18个六边形镜面组成。韦布望远镜将研究宇宙早期形成的第一批星系、恒星形成区域以及系外行星大气。它位于距地球约150万公里的L2点，预计工作寿命至少10年。",
                    Category = "天文仪器",
                    Tags = new List<string> { "太空望远镜", "红外天文学", "宇宙早期" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "18",
                    Title = "甚大望远镜计划",
                    Summary = "下一代地基光学巨型望远镜的发展",
                    Content = "甚大望远镜(ELT)是正在建设的新一代地基光学望远镜，主镜直径达39米，由798个六边形镜片组成。完工后它将是世界上最大的光学/红外望远镜，其集光能力和分辨率将大大超过现有望远镜。ELT的科学目标包括直接成像系外行星、研究第一批星系和黑洞，以及直接测量宇宙加速膨胀。",
                    Category = "天文仪器",
                    Tags = new List<string> { "光学望远镜", "自适应光学", "天文观测" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "19",
                    Title = "引力波天文台",
                    Summary = "探测时空涟漪的新型天文设施",
                    Content = "引力波天文台如LIGO和Virgo通过激光干涉技术探测引力波。这些设施能够测量小于原子核的距离变化，用于捕捉由黑洞或中子星合并等剧烈事件产生的时空涟漪。2015年首次直接探测到引力波是物理学重大突破，开创了多信使天文学时代，使我们能够观测不发光的天体和宇宙早期。",
                    Category = "天文仪器",
                    Tags = new List<string> { "引力波", "LIGO", "多信使天文学" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "20",
                    Title = "快速射电暴之谜",
                    Summary = "近年发现的神秘宇宙无线电信号爆发",
                    Content = "快速射电暴(FRB)是持续几毫秒的强烈射电脉冲，首次发现于2007年。其能量巨大，可能来自数十亿光年外。部分FRB具有重复性，表明它们并非源于天体破坏性事件。目前主流理论认为中子星(尤其是磁星)可能是FRB的来源，但确切机制仍未确定。",
                    Category = "最新发现",
                    Tags = new List<string> { "FRB", "射电天文学", "磁星" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "21",
                    Title = "暗能量的发现与谜团",
                    Summary = "宇宙加速膨胀背后的神秘能量",
                    Content = "暗能量是1998年通过对Ia型超新星的观测发现的，它推动宇宙加速膨胀，约占宇宙能量密度的68%。暗能量的本质仍然神秘，可能是宇宙学常数(代表真空能量)，也可能是一种随时间变化的能量场，如第五种基本力。理解暗能量是现代宇宙学最大挑战之一。",
                    Category = "最新发现",
                    Tags = new List<string> { "暗能量", "宇宙膨胀", "宇宙学常数" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                },
                new KnowledgeItem
                {
                    Id = "22",
                    Title = "TRAPPIST-1系统",
                    Summary = "拥有七颗类地行星的引人注目恒星系统",
                    Content = "TRAPPIST-1是一个距离地球约40光年的红矮星系统，拥有七颗大小与地球相近的行星(b到h)。其中三到四颗位于宜居带内，可能存在液态水。这些行星围绕恒星轨道紧密，互相之间引力作用强烈，形成了罕见的共振轨道链。这是目前发现的拥有最多类地行星的单一恒星系统。",
                    Category = "最新发现",
                    Tags = new List<string> { "系外行星", "宜居带", "红矮星" },
                    IsFavorite = false,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ImagePath = null
                }
            };
        }

        public List<KnowledgeItem> GetAllItems()
        {
            return _items.ToList();
        }

        public List<KnowledgeItem> GetItemsByCategory(string category)
        {
            return _items.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<KnowledgeItem> SearchItems(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<KnowledgeItem>();

            return _items.Where(item => 
                item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                item.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                item.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        public List<KnowledgeItem> GetFavoriteItems()
        {
            return _items.Where(item => item.IsFavorite).ToList();
        }

        public List<string> GetAllCategories()
        {
            return _items.Select(item => item.Category)
                         .Distinct()
                         .OrderBy(c => c)
                         .ToList();
        }

        public void AddToFavorites(string itemId)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.IsFavorite = true;
            }
        }

        public void RemoveFromFavorites(string itemId)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.IsFavorite = false;
            }
        }

        public KnowledgeItem GetItemById(string id)
        {
            return _items.FirstOrDefault(item => item.Id == id) ?? new KnowledgeItem();
        }
    }
} 
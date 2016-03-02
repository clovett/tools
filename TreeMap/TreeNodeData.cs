using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft;
using Newtonsoft.Json;
using System.IO;
using System.Windows;

namespace TreeMaps
{
    public class TreeNodeData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("color")]
        public Color Color { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("children")]
        public List<TreeNodeData> Children { get; set; }

        #region layout info
        private double _area;
        private Size _desiredSize;
        private Point _desiredLocation;
        private double _ratio;
        private UIElement _ui;
        private TreeNodeData _parent;
        #endregion 

        public TreeNodeData(string id, string label)
        {
            this.Id = id;
            this.Label = label;
        }

        public void SetColor(int dimension, Color color)
        {
            this.Color = color;
        }

        public void AddSize(int dimension, double size)
        {
            this.Size += size;
        }

        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        public static TreeNodeData Load(string file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                string json = reader.ReadToEnd();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TreeNodeData>(json);
            }
        }

        public void Save(string file)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.WriteLine(this.ToString());
            }
        }

        public TreeNodeData GetOrCreateNode(string id, string label)
        {
            if (Children == null)
            {
                Children = new List<TreeNodeData>();
            }
            TreeNodeData result = (from n in Children where n.Id == id select n).FirstOrDefault();
            if (result == null)
            {
                result = new TreeNodeData(id, label);
                result._parent = this;
                Children.Add(result);
            }
            return result;
        }


        #region properties

        [JsonIgnore]
        internal Size ComputedSize
        {
            get { return _desiredSize; }
            set { _desiredSize = value; }
        }

        [JsonIgnore]
        internal Point ComputedLocation
        {
            get { return _desiredLocation; }
            set { _desiredLocation = value; }
        }
        [JsonIgnore]
        internal double AspectRatio
        {
            get { return _ratio; }
            set { _ratio = value; }
        }
        [JsonIgnore]
        internal double Weight
        {
            get { return this.Size; }
        }
        [JsonIgnore]
        internal double RealArea
        {
            get { return _area; }
            set { _area = value; }
        }

        [JsonIgnore]
        internal UIElement UiElement
        {
            get { return _ui; }
            set { _ui = value; }
        }

        [JsonIgnore]
        public TreeNodeData Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }


        #endregion

        #region static members

        internal static int CompareByValueDecreasing(TreeNodeData x, TreeNodeData y)
        {
            if (x == null)
            {
                if (y == null)
                    return -1;
                else
                    return 0;
            }
            else
            {
                if (y == null)
                    return 1;
                else
                    return x.Weight.CompareTo(y.Weight) * -1;
            }
        }

        #endregion
    }
}

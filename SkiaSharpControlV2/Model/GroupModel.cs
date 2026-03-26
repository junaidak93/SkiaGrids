using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaSharpControlV2.Model
{
    public class GroupModel
    {
        public bool IsGroupHeader { get; set; }
        public string? GroupName { get; set; }
        public bool IsExpanded { get; set; } = true;
        public object? Item { get; set; }
    }
}

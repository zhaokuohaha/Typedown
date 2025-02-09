﻿namespace Typedown.Core.Models.RuntimeModels
{
    public class HtmlImgTag
    {
        public string Src { get; set; }

        public string Title { get; set; }

        public string Alt { get; set; }

        public HtmlImgTag(string src, string alt = null, string title = null)
        {
            Src = src;
            Alt = alt;
            Title = title;
        }
    }
}

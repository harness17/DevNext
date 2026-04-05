using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// SelectList関連Utility
    /// </summary>
    public static class SelectListUtility
    {
        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = area,
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>(List<T> obj) where T : struct
        {
            var list = new List<SelectListItem>();
            var targetAreas = obj.Select(s => s.ToString()).ToList();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                if (targetAreas.Contains(area))
                {
                    list.Add(new SelectListItem { Value = area, Text = EnumUtility.GetEnumDisplay<T>(area) });
                }
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemToSubValue<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = EnumUtility.GetEnumSubValue<T>(area),
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemOrder<T>() where T : struct
        {
            var sortlist = Enum.GetNames(typeof(T))
                .Select(area => new { Name = area, Text = EnumUtility.GetEnumDisplay<T>(area), Order = EnumUtility.GetEnumDisplayOrder<T>(area) })
                .OrderBy(x => x.Order)
                .ToList();

            return sortlist.Select(area => new SelectListItem { Value = area.Name, Text = area.Text }).ToList();
        }

        public static IEnumerable<SelectListItem> GetNumberSelectList(int startNumber, int maxNumber, int step = 1, string format = "")
        {
            var list = new List<SelectListItem>();
            for (int i = startNumber; i <= maxNumber; i += step)
            {
                list.Add(new SelectListItem { Value = i.ToString(format), Text = i.ToString(format) });
            }
            return list;
        }
    }
}

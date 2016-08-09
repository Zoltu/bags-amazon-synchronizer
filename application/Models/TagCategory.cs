using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace application.Models
{
    public class TagCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public String Name { get; set; }

        public List<Tag> Tags { get; set; } = new List<Tag>();

        public dynamic ToBaseWireFormat()
        {
            dynamic result = new ExpandoObject();
            result.id = Id;
            result.name = Name;
            return result;
        }

        public dynamic ToUnsafeExpandedWireFormat()
        {
            var result = ToBaseWireFormat();
            result.tags = Tags.Select(tag => tag.ToSafeExpandedWireFormat()).ToList();
            return result;
        }
    }

    public static class TagCategoryExtensions
    {
        public static IQueryable<TagCategory> WithUnsafeIncludes(this IQueryable<TagCategory> query)
        {
            return query
                .Include(category => category.Tags);
        }
    }
}

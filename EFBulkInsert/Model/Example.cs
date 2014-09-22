using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFBulkInsert.Model
{
    [Table("Example")]
    public class Example
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(500)]
        [DataType("nvarchar")]
        public string Description { get; set; }

        [ConcurrencyCheck]
        [DataType("datetimeoffset")]
        public DateTime LastModified { get; set; }
    }
}

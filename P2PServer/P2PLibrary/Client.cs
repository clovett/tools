namespace P2PLibrary
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Client
    {
        [Key]
        [StringLength(256)]
        public string Name { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(50)]
        public string LocalAddress { get; set; }

        [Required]
        [StringLength(50)]
        public string LocalPort { get; set; }

        [Required]
        [StringLength(50)]
        public string RemoteAddress { get; set; }

        [Required]
        [StringLength(50)]
        public string RemotePort { get; set; }

        [NotMapped]
        public string Message { get; set; }
    }
}

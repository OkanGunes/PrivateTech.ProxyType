using System;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace PrivateTech.ProxyType.Test
{
    public class ProxyTypeBuilderTest
    {
        [Fact]
        public void GetInstanceTest()
        {
            var person = ProxyTypeBuilder.GetInstace<IPerson>();
            person.Name = "Okan";
            person.Surname = "Gunes";
        }

        public interface IPerson
        {
            [Required(ErrorMessage = "Name is required")]
            [MaxLength(10)]
            string Name { get; set; }

            string Surname { get; set; }
        }
    }
}

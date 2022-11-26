namespace Elastic_Simple_Search.Test.Models
{
    /// <summary>
    /// Models: Person
    /// </summary>
    public class Person
    {

        /// <summary>
        /// E-Mail Suffix
        /// </summary>
        public const string EmailSuffix = "nomail.org";

        /// <summary>
        /// PK
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// E-Mail
        /// </summary>
        public string EMail { get; set; }

        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// To String (debug)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{this.Id}] {this.FirstName} {this.LastName}, {this.EMail}, {this.Phone}";
        }

        /// <summary>
        /// Make Random Person
        /// </summary>
        /// <returns>Person</returns>
        public static Person MakeRandom(long index)
        {
            var p = new Person
            {
                Id = index,
                FirstName = Faker.Name.FirstName(),
                LastName = Faker.Name.LastName(),
                Phone = Faker.Phone.GetPhoneNumber()
            };

            p.EMail = $"{p.FirstName}.{p.LastName}@{EmailSuffix}";

            return p;
        }
    }
}

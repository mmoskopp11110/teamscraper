namespace Spielerplus.Data
{
    /// <summary>
    /// a spielerplus user entity
    /// </summary>
    public class User
    {
        /// <summary>
        /// construct user with id and name
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="name"></param>
        public User(string uid, string name)
        {
            Id = uid;
            Name = name;
        }
        
        /// <summary>
        /// unique spielerplus user id
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// name of the user as: Lastname Firstname
        /// </summary>
        public string Name { get; private set; }
    }
}
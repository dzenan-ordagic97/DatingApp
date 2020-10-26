namespace DatingApp.API.Models
{
    public class Like
    {
        public int LikerID { get; set; }
        public int LikeeID { get; set; }
        public User Liker { get; set; }
        public User Likee { get; set; }

    }
}
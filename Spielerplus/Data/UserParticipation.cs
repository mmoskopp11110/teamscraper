namespace Spielerplus.Data
{
    /// <summary>
    /// participation status enumeration
    /// </summary>
    public enum Participation
    {
        Unassigned,
        Going,
        Unsafe,
        Absent,
        NotNominated
    }
    
    /// <summary>
    /// connect user, participation status and reason for this status
    /// </summary>
    public class UserParticipation
    {
        public User User { get; set; }
        public Participation Participation { get; set; }
        public string Reason { get; set; }
    }
}
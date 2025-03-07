using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsACSFunctions
{
    /// <summary>
    /// Represents a ticket with details such as description, date, user ID, and recipient.
    /// </summary>
    public class Ticket
    {
        /// <summary>
        /// Gets or sets the description of the ticket.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the date of the ticket.
        /// </summary>
        public string? Date { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with the ticket.
        /// </summary>
        public string? UserID { get; set; }

        /// <summary>
        /// Gets or sets the recipient of the ticket.
        /// </summary>
        public string? Recipient { get; set; }
    }

    /// <summary>
    /// Represents the payload for communication notifications.
    /// </summary>
    public class CommsNotificationsPayload
    {
        /// <summary>
        /// Gets or sets the call ID.
        /// </summary>
        public string CallId { get; set; }

        /// <summary>
        /// Gets or sets the scenario ID.
        /// </summary>
        public string ScenarioId { get; set; }

        /// <summary>
        /// Gets or sets the application ID.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the state of the call.
        /// </summary>
        public CallState CallState { get; set; }

        /// <summary>
        /// Gets or sets the resource data.
        /// </summary>
        public ResourceData ResourceData { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public string TimeStamp { get; set; }
    }


    /// <summary>
    /// Represents the state of a call.
    /// </summary>
    public class CallState
    {
        /// <summary>
        /// Gets or sets the state of the call (e.g., "Established", "Terminated").
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the direction of the call ("Incoming" or "Outgoing").
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// Gets or sets the participants in the call.
        /// </summary>
        public ParticipantInfo[] Participants { get; set; }
    }

    /// <summary>
    /// Represents information about a participant in a call.
    /// </summary>
    public class ParticipantInfo
    {
        /// <summary>
        /// Gets or sets the identity of the participant.
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Gets or sets the display name of the participant.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the role of the participant.
        /// </summary>
        public string Role { get; set; }
    }

    /// <summary>
    /// Represents resource data associated with a call.
    /// </summary>
    public class ResourceData
    {
        /// <summary>
        /// Gets or sets the ID of the resource.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the call chain IDs associated with the resource.
        /// </summary>
        public string[] CallChainId { get; set; }
    }

}

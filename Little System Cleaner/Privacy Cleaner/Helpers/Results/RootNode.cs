using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class RootNode : ResultNode
    {
        /// <summary>
        /// Constructor for root node
        /// </summary>
        public RootNode()
        {

        }

        /// <summary>
        /// Constructor for root node
        /// </summary>
        /// <param name="sectionName">Section Name</param>
        public RootNode(string sectionName)
        {
            Section = sectionName;
        }

        public override void Clean(Report report)
        {
            // Nothing to do here
        }
    }
}

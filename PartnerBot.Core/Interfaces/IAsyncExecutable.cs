using System.Threading.Tasks;

namespace PartnerBot.Core.Interfaces
{
    public interface IAsyncExecutable
    {
        /// <summary>
        /// Represents a class that is designed to have a single function, which this method executes.
        /// </summary>
        /// <returns>A task that contains the execution of this class.</returns>
        public Task ExecuteAsync();
    }
}

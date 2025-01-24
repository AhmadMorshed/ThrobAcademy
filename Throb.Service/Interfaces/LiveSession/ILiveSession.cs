using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;

namespace Throb.Service.Interfaces
{
    public interface ILiveSession
    {
        LiveSession GetById(int? id);
        IEnumerable<LiveSession> GetAll();
        void Add(LiveSession liveSession);
        void Update(LiveSession liveSession);
        void Delete(LiveSession liveeSession);

    }
}

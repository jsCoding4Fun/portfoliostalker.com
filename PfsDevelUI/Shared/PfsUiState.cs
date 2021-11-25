using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PfsDevelUI.Shared
{
    public class PfsUiState
    {
        public event Action OnMenuUpdated;

        public void UpdateNavMenu() => OnMenuUpdated?.Invoke();
    }
}

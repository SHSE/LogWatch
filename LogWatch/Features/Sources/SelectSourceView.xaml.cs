using System;
using System.Net;

namespace LogWatch.Features.Sources {
    public partial class SelectSourceView {
        public static readonly Func<IPEndPoint> SelectEndPoint = () => {
            var view = new SelectPortView();

            if (view.ShowDialog() == true)
                return new IPEndPoint(IPAddress.Any, view.ViewModel.Port.GetValueOrDefault());

            return null;
        };

        public SelectSourceView() {
            this.InitializeComponent();
        }

        public SelectSourceViewModel ViewModel {
            get { return (SelectSourceViewModel) this.DataContext; }
        }
    }
}
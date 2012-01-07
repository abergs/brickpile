using BrickPile.Domain.Models;
using Raven.Client.Listeners;
using Raven.Json.Linq;

namespace BrickPile.Core.Infrastructure.Listeners {
    /// <summary>
    /// 
    /// </summary>
    internal class StoreListener : IDocumentStoreListener {
        /// <summary>
        /// Invoked before the store request is sent to the server.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entityInstance">The entity instance.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>
        /// Whatever the entity instance was modified and requires us re-serialize it.
        /// Returning true would force re-serialization of the entity, returning false would
        /// mean that any changes to the entityInstance would be ignored in the current SaveChanges call.
        /// </returns>
        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata) {
            if(entityInstance is IPageModel) {
                var model = entityInstance as IPageModel;
                Application.Instance.OnSavingPage(new PageModelEventArgs(model));
            }
            return true;
        }
        public void AfterStore(string key, object entityInstance, RavenJObject metadata) {
            if (entityInstance is IPageModel) {
                var model = entityInstance as IPageModel;
                Application.Instance.OnSavedPage(new PageModelEventArgs(model));
            }
            
        }
    }
}
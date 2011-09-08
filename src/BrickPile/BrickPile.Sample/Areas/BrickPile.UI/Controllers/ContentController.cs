﻿/* Copyright (C) 2011 by Marcus Lindblom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrickPile.Core.Exception;
using BrickPile.Core.Repositories;
using BrickPile.Domain.Models;
using BrickPile.UI.Models;
using BrickPile.UI.Web.ViewModels;
using Raven.Client.Document;

namespace BrickPile.UI.Controllers {
    [Authorize]
    public class ContentController : Controller {
        
        private readonly dynamic _model;
        private readonly IRepository<IPageModel> _repository;
        private readonly IStructureInfo _structureInfo;
        /// <summary>
        /// Default action
        /// </summary>
        /// <returns>
        /// Redirects to the Edit action with the home page loaded
        /// </returns>
        public ActionResult Index() {
            //var viewModel = new DashboardViewModel(_model, _structureInfo);
            //return View(viewModel);

            if (_model != null && _model is IPageModel) {
                var viewModel = new DashboardViewModel(_model, _structureInfo);
                return View("Index", viewModel);
            }

            return View("Start", new DefaultPageModel());
        }
        /// <summary>
        /// Responsible for providing the Edit view with data from the current page
        /// </summary>
        /// <returns></returns>
        public ActionResult Edit() {
            var viewModel = new DashboardViewModel(_model, _structureInfo);
            return View(viewModel);
        }
        /// <summary>
        /// Responsible for saving all changes made to the current page
        /// </summary>
        /// <param name="editorModel">The editor model.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public virtual ActionResult Update(dynamic editorModel, dynamic model) {

            if (!TryUpdateModel(model, "CurrentModel")) {
                return View("edit", new DashboardViewModel(model, _structureInfo));
            }
            
            UpdateModel(model);

            model.Metadata.Changed = DateTime.Now;
            model.Metadata.ChangedBy = HttpContext.User.Identity.Name;

            _repository.SaveChanges();
            _repository.Refresh(model);

            var page = model as IPageModel;
            var parent = _repository.SingleOrDefault<IPageModel>(m => m.Id == page.Parent.Id);

            return RedirectToAction("index", new { model = parent });
        }
        /// <summary>
        /// Responsible for providing the add page view with data
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public ActionResult Add(dynamic model) {
            return PartialView(new CreateNewModel
                                   {
                                       CurrentModel = model,
                                       BackAction = "edit",
                                       Url = VirtualPathUtility.AppendTrailingSlash(_model.Metadata.Url)
                                   });
        }
        /// <summary>
        /// News the specified new page model.
        /// </summary>
        /// <param name="newPageModel">The new page model.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public ActionResult New(CreateNewModel newPageModel, dynamic model) {
            var parent = model as IPageModel;
            if (parent == null) {
                throw new BrickPileException("The injected model is not a PageModel");
            }

            if (ModelState.IsValid) {
                // create a new page from the selected page model
                var page = Activator.CreateInstance(Type.GetType(newPageModel.SelectedPageModel)) as IPageModel;
                if (page == null) {
                    throw new BrickPileException("The selected page model is not valid!");
                }
                page.Metadata.Url = VirtualPathUtility.AppendTrailingSlash(parent.Metadata.Url);
                
                ViewBag.Class = "content";
                return View("new", new NewPageViewModel { NewPageModel = page, CurrentModel = parent, StructureInfo = _structureInfo });
            }

            return PartialView("add", newPageModel);
        }
        [HttpPost]
        [ValidateInput(false)]
        public virtual ActionResult Save(dynamic newPageModel, dynamic model) {

            var parent = model as IPageModel;
            if (parent == null) {
                throw new BrickPileException("The injected model is not a PageModel");
            }

            if (ModelState.IsValid) {
                // create a new page from the new model
                var page = Activator.CreateInstance(Type.GetType(Request.Form["AssemblyQualifiedName"])) as IPageModel;

                if (page == null) {
                    throw new BrickPileException("The selected page model is not valid!");
                }

                // Update all values
                UpdateModel(page, "NewPageModel");
                // Set the parent
                page.Parent = model;
                // Set changed date
                page.Metadata.Changed = DateTime.Now;
                page.Metadata.ChangedBy = HttpContext.User.Identity.Name;

                // Add page to repository and save changes
                _repository.Store(page);
                _repository.SaveChanges();
                
                return RedirectToAction("index", new { model = parent });
            }

            return null;
        }
        /// <summary>
        /// Publishes this instance.
        /// </summary>
        /// <returns></returns>
        public virtual ActionResult Publish(string id, bool published) {
            var model = _repository.SingleOrDefault<IPageModel>(m => m.Id == id.Replace("_","/"));
            model.Metadata.PublishedStatus = published;
            model.Metadata.Changed = DateTime.Now;
            _repository.SaveChanges();

            return Content(published != true
                            ? "Hooray, you're page has been unpublished"
                            : "Hooray, you're page has been published");
        }
        /// <summary>
        /// Responsible for creating a new page based on the selected page model
        /// </summary>
        /// <param name="newPageModel">The new page model.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [Obsolete]
        public ActionResult Create(CreateNewModel newPageModel, dynamic model) {
            var parent = model as IPageModel;
            if(parent == null) {
                throw new BrickPileException("The injected model is not a PageModel");
            }

            if (ModelState.IsValid) {
                // create a new page from the selected page model
                var page = Activator.CreateInstance(Type.GetType(newPageModel.SelectedPageModel)) as IPageModel;
                // handle this gracefully in the future :)
                if (page == null) {
                    throw new BrickPileException("The selected page model is not valid!");
                }

                page.Parent = model;
                page.Metadata.Name = newPageModel.Name;
                page.Metadata.Slug = newPageModel.Slug;
                page.Metadata.Url = newPageModel.Url;

                _repository.Store(page);
                _repository.SaveChanges();
                _repository.Refresh(page);

                return RedirectToAction("edit", new { model = page });
            }
            return PartialView("add", newPageModel);
        }
        /// <summary>
        /// Creates the default.
        /// </summary>
        /// <param name="defaultPageModel">The default page model.</param>
        /// <returns></returns>
        public ActionResult CreateDefault(DefaultPageModel defaultPageModel) {
            if(ModelState.IsValid) {
                // create a new page from the selected page model
                var page = Activator.CreateInstance(Type.GetType(defaultPageModel.SelectedPageModel)) as IPageModel;
                // handle this gracefully in the future :)
                if (page == null) {
                    throw new BrickPileException("The selected page model is not valid!");
                }
                page.Metadata.Name = defaultPageModel.Name;
                _repository.Store(page);
                _repository.SaveChanges();

                return RedirectToAction("edit", new { model = page });
            }
            return View("index", defaultPageModel);
        }
        /// <summary>
        /// Deletes the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Delete(IPageModel model) {
            return PartialView("Confirm", new ConfirmFormModel()
                                       {
                                           BackAction = "edit",
                                           CurrentModel = model                                           
                                       } );
        }
        /// <summary>
        /// Pastes the specified source id.
        /// </summary>
        /// <param name="sourceId">The source id.</param>
        /// <param name="destinationId">The destination id.</param>
        /// <returns></returns>
        //public ActionResult Paste(string sourceId, string destinationId) {

        //    dynamic source = _repository.SingleOrDefault<IPageModel>(x => x.Id.Equals(sourceId));
        //    dynamic destination = _repository.SingleOrDefault<IPageModel>(x => x.Id.Equals(destinationId));

        //    source.Parent = destination;
        //    _repository.SaveChanges();

        //    return RedirectToAction("Index");

        //}
        /// <summary>
        /// Deletes the specified confirm form model.
        /// </summary>
        /// <param name="confirmFormModel">The confirm form model.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Delete(ConfirmFormModel confirmFormModel, IPageModel model) {

            _repository.Delete(model);
            _repository.SaveChanges();

            if (model.Parent != null) {
                var parent = _repository.SingleOrDefault<IPageModel>(x => x.Id.Equals(model.Parent.Id));
                return RedirectToAction("edit", new { model = parent });
            }
            return RedirectToAction("index");            
        }
        /// <summary>
        /// Initializes a new instance of the <b>PagesController</b> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="structureInfo">The structure info.</param>
        /// <param name="repository">The repository.</param>
        public ContentController(IPageModel model, IStructureInfo structureInfo, IRepository<IPageModel> repository) {
            _model = model;
            _repository = repository;
            _structureInfo = structureInfo;
        }
    }
}

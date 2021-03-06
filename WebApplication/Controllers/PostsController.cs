using System;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication.Extensions;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    public class PostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Posts
        public ActionResult Index()
        {
          
            var postsWithAuthors = db.Posts.
                Include(p => p.Author).ToList(); 
            return View(postsWithAuthors.OrderByDescending(p => p.Date).ToList());
        }

        // GET: Posts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = db.Posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            
            return View(post);
        }

        // GET: Posts/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.Tags = db.Tags.ToList();
            ViewBag.Categories = db.Categories.ToList();
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Create([Bind(Include = "Id,Title,Body,Date,Author_Id")] Post post,
            string category,  string [] tags, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Files/"), fileName);
                        file.SaveAs(path);
                        post.FileName = fileName;
                    }
                    ViewBag.Message = "Upload successful";

                }
                catch
                {
                    ViewBag.Message = "Upload failed";

                }
               
                post.Author = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                post.CommentsCount = 0;
                post.Date = DateTime.Now;
                
                if(tags != null)
                {
                    foreach (var tagId in tags)
                    {
                        var id = Convert.ToInt32(tagId);
                        var tag = db.Tags.Find(id);
                        post.Tags.Add(tag);
                        db.Tags.Find(id).Posts.Add(post);
                    }
                }
                var categoryId = Convert.ToInt32(category);
                var postCategory = db.Categories.Find(categoryId);
                post.Category = postCategory;

                db.Users.Find(post.Author.Id).Posts.Add(post);
                foreach (var userPost in db.Users.Find(post.Author.Id).Posts)
                {
                    var p = userPost;
                }
                db.Categories.Find(categoryId).Posts.Add(post);
                db.Posts.Add(post);
                db.SaveChanges();
                this.AddNotification("???????????????? ?? ?????????????? ??????????????????.", NotificationType.SUCCESS);
                return RedirectToAction("Index");
            }
            this.AddNotification("????????????! ?????????? ???? ???????????????? ???? ?? ??????????????????.", NotificationType.ERROR);
            return RedirectToAction("/Create");
        }

        // GET: Posts/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            var currentPost = db.Posts.Single(p => p.Id == id);
            var currentUser = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

            if (User.Identity.Name == currentPost.Author.UserName || 
               currentUser.Role == "Admin")
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Post post = db.Posts.Find(id);
                if (post == null)
                {
                    return HttpNotFound();
                }
                return View(post);
            }
            return RedirectToAction("Index");
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit( Post post, string Title, string Body, int? id)
        {
            if (ModelState.IsValid)
            {
                post = db.Posts.Find(id);
                post.Title = Title;
                post.Body = Body;
                db.Entry(post).State = EntityState.Modified;
                db.SaveChanges();
                this.AddNotification("???????????????? ?? ?????????????? ??????????????????????.", NotificationType.SUCCESS);
                return RedirectToAction("Index");
            }
            this.AddNotification("????????????! ?????????? ???? ???????????????? ???? ?? ?????????????????? ????????????????.", NotificationType.ERROR);
            return RedirectToAction("/Edit/" + id);
        }

        // GET: Posts/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            var currentPost = db.Posts.Single(p => p.Id == id);
            var currentUser = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

            if (User.Identity.Name == currentPost.Author.UserName ||
               currentUser.Role == "Admin")
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Post post = db.Posts.Find(id);
                if (post == null)
                {
                    return HttpNotFound();
                }
                return View(post);
            }
            return RedirectToAction("Index");
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(int id)
        {
            Post post = db.Posts.Find(id);
            foreach (Comment comment in post.Comments.ToList())
            {
                db.Comments.Remove(comment);
            }
            foreach (var tag in post.Tags.ToList())
            {
                var tagId = tag.Id;
                db.Tags.Find(tagId).Posts.Remove(post);
            }
            db.Users.Find(post.Author.Id).Posts.Remove(post);
            db.Posts.Remove(post);
            db.SaveChanges();
            this.AddNotification("???????????????? ?? ??????????????.", NotificationType.INFO);
            return RedirectToAction("Index");
        }
        public FileResult Download(string ImageName)
        {
            return File("~/Files/" + ImageName, System.Net.Mime.MediaTypeNames.Application.Octet, ImageName);

        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}


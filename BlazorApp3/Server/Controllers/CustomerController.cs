﻿using BlazorApp3.Shared;
using Braintree;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlazorApp3.Server.Controllers;
[ApiController]
[Route("[controller]")]
//[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    Censor censor;
    public CustomerController(Censor censor)
    {
        this.censor = censor;
    }
    [HttpGet("Movie/{searchString?}/{sortOrder?}/{index:int:min(0)}")]
    public async Task<ActionResult<List<MovieModel>>> Movie(string? sortOrder, string? searchString, int index)
    {
        try
        {
            FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");

            Query usersRef = db.Collection("Movie");
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                usersRef = usersRef.OrderBy("MovieName").StartAt(searchString).EndAt(searchString + "~");
            }

            switch (sortOrder)
            {
                case "name":
                    usersRef = usersRef.OrderBy("MovieName");
                    break;
                case "nameDesc":
                    usersRef = usersRef.OrderByDescending("MovieName");
                    break;
                case "date":
                    usersRef = usersRef.OrderBy("PremiereDate");
                    break;
                case "dateDesc":
                    usersRef = usersRef.OrderByDescending("PremiereDate");
                    break;
                case "genre":
                    usersRef = usersRef.OrderBy("MovieGenre");
                    break;
                case "genreDesc":
                    usersRef = usersRef.OrderByDescending("MovieGenre");
                    break;
            }

            List<MovieModel> myFoo = new();
            usersRef = usersRef.Offset(index * 5).Limit(5);
            QuerySnapshot snapshot = await usersRef.GetSnapshotAsync();


            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                myFoo.Add(document.ConvertTo<MovieModel>());
            }

            return await Task.FromResult(myFoo);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

    }
    [HttpGet("Watch/{Id}")]
    public async Task<ActionResult<MovieModel>> Watch(string Id)
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
        Query collection = db.Collection("Movie").WhereEqualTo("MovieId", Id);

        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        MovieModel movie = new();

        foreach (DocumentSnapshot snapshotDocument in snapshot.Documents)
        {
            movie = snapshotDocument.ConvertTo<MovieModel>();
        }

        return await Task.FromResult(movie);
    }
    //[HttpGet("Deposit")]
    //public IActionResult Deposit(string Cash, string clientToken)
    //{
    //    if (Cash == null)
    //    {
    //        return View();
    //    }

    //    BraintreeGateway gateway = new()
    //    {
    //        Environment = Braintree.Environment.SANDBOX,
    //        MerchantId = "ts2vvzhy3dzc2dzc",
    //        PublicKey = "mg2whqyw68xxm3g9",
    //        PrivateKey = "b12f9762c30b546c29e5172fd3729c0d"
    //    };
    //    if (clientToken == null)
    //    {
    //        clientToken = gateway.ClientToken.Generate();
    //        ViewBag.clientToken = clientToken;
    //        ViewBag.Cash = Cash;
    //        return View();
    //    }

    //    return View();
    //}
    [HttpGet("Deposit")]
    public async Task<ActionResult<char[]>> Deposit()
    {

        string clientToken;

        BraintreeGateway gateway = new()
        {
            Environment = Braintree.Environment.SANDBOX,
            MerchantId = "ts2vvzhy3dzc2dzc",
            PublicKey = "mg2whqyw68xxm3g9",
            PrivateKey = "b12f9762c30b546c29e5172fd3729c0d"
        };
        clientToken = gateway.ClientToken.Generate();
        return clientToken.ToCharArray();
    }
    [HttpPost("DoCard")]
    public async Task<ActionResult> DoCard([FromForm] string nonce, [FromForm] string cash)
    {
        TransactionRequest request = new()
        {
            Amount = Convert.ToDecimal(cash),
            PaymentMethodNonce = nonce,
            Options = new TransactionOptionsRequest { SubmitForSettlement = true }
        };
        BraintreeGateway gateway = new()
        {
            Environment = Braintree.Environment.SANDBOX,
            MerchantId = "ts2vvzhy3dzc2dzc",
            PublicKey = "mg2whqyw68xxm3g9",
            PrivateKey = "b12f9762c30b546c29e5172fd3729c0d"
        };
        Result<Braintree.Transaction> result = gateway.Transaction.Sale(request);


        if (result.IsSuccess())
        {
            Braintree.Transaction transaction = result.Target;
            FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
            Query usersRef = db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid));

            QuerySnapshot snapshotAsync = await usersRef.GetSnapshotAsync();


            foreach (DocumentSnapshot snapshotAsyncDocument in snapshotAsync.Documents)
            {
                await snapshotAsyncDocument.Reference.UpdateAsync(
                    new Dictionary<string, dynamic> {
                            {
                                "Wallet", Convert.ToDouble(transaction.Amount) +
                                          snapshotAsyncDocument.ConvertTo<AccountManagementModel>().Wallet
                            }
                    });
            }

            ModelState.AddModelError(string.Empty, "Success!: " + transaction.Id);
            string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            return Redirect($"{hostname}/Deposit/Success");
        }
        else if (result.Transaction != null)
        {
            Braintree.Transaction transaction = result.Transaction;

            ModelState.AddModelError(string.Empty, "Error processing transaction:");
            ModelState.AddModelError(string.Empty, "  Status: " + transaction.Status);
            ModelState.AddModelError(string.Empty, "  Code: " + transaction.ProcessorResponseCode);
            ModelState.AddModelError(string.Empty, "  Text: " + transaction.ProcessorResponseText);
            string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            return Redirect($"{hostname}/Deposit/Error processing transaction");
        }
        else
        {
            foreach (ValidationError error in result.Errors.DeepAll())
            {
                ModelState.AddModelError(string.Empty, "Attribute: " + error.Attribute);
                ModelState.AddModelError(string.Empty, "  Code: " + error.Code);
                ModelState.AddModelError(string.Empty, "  Message: " + error.Message);

                string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

                return Redirect($"{hostname}/Deposit/{error.Message}");
            }
        }

        return View();
    }
    [HttpPost("DoPayPal")]
    public async Task<ActionResult> DoPaypal([FromForm] string nonce, [FromForm] string cash)
    {
        BraintreeGateway gateway = new()
        {
            Environment = Braintree.Environment.SANDBOX,
            MerchantId = "ts2vvzhy3dzc2dzc",
            PublicKey = "mg2whqyw68xxm3g9",
            PrivateKey = "b12f9762c30b546c29e5172fd3729c0d"
        };

        TransactionRequest request = new()
        {
            Amount = Convert.ToDecimal(cash),
            PaymentMethodNonce = nonce,
            OrderId = "Mapped to PayPal Invoice Number",
            Options = new TransactionOptionsRequest
            {
                SubmitForSettlement = true,
                PayPal = new TransactionOptionsPayPalRequest
                {
                    CustomField = "PayPal custom field",
                    Description = "Description for PayPal email receipt"
                }
            }
        };

        Result<Braintree.Transaction> result = gateway.Transaction.Sale(request);
        if (result.IsSuccess())
        {
            Braintree.Transaction transaction = result.Target;
            FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
            Query usersRef = db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid));

            QuerySnapshot snapshotAsync = await usersRef.GetSnapshotAsync();


            foreach (DocumentSnapshot snapshotAsyncDocument in snapshotAsync.Documents)
            {
                await snapshotAsyncDocument.Reference.UpdateAsync(
                    new Dictionary<string, dynamic> {
                            {
                                "Wallet", Convert.ToDouble(transaction.Amount) +
                                          snapshotAsyncDocument.ConvertTo<AccountManagementModel>().Wallet
                            }
                    });
            }

            ModelState.AddModelError(string.Empty, "Success!: " + transaction.Id);
            string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            return Redirect($"{hostname}/Deposit/Success");
        }
        else if (result.Transaction != null)
        {
            Braintree.Transaction transaction = result.Transaction;

            ModelState.AddModelError(string.Empty, "Error processing transaction:");
            ModelState.AddModelError(string.Empty, "  Status: " + transaction.Status);
            ModelState.AddModelError(string.Empty, "  Code: " + transaction.ProcessorResponseCode);
            ModelState.AddModelError(string.Empty, "  Text: " + transaction.ProcessorResponseText);
            string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            return Redirect($"{hostname}/Deposit/Error processing transaction");
        }
        else
        {
            foreach (ValidationError error in result.Errors.DeepAll())
            {
                ModelState.AddModelError(string.Empty, "Attribute: " + error.Attribute);
                ModelState.AddModelError(string.Empty, "  Code: " + error.Code);
                ModelState.AddModelError(string.Empty, "  Message: " + error.Message);
                string hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

                return Redirect($"{hostname}/Deposit/{error.Message}");
            }
        }

        return View();
    }
    [HttpGet("VipCheck")]
    public async Task<ActionResult<char[]>> VipCheck()
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
        QuerySnapshot vipCheck = await db.Collection("Vip").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync();
        if (vipCheck.Documents.Count == 0)
        {


            return await Task.FromResult("Not Vip".ToCharArray());
        }
        else
        {
            if (vipCheck.Documents[0].GetValue<DateTime>("Time") < DateTime.UtcNow)
            {

                return await Task.FromResult("Expires Vip".ToCharArray());
            }
            else
            {

                return await Task.FromResult(vipCheck.Documents[0].GetValue<DateTime>("Time").ToString().ToCharArray());
            }

        }
    }
    [HttpPost("BuyVip")]
    public async Task<ActionResult> BuyVip([FromBody] VipModel vip)
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
        CollectionReference collection = db.Collection("Buy");
        if (vip.Choose == 0 && vip.Id != null)
        {
            if ((await db.Collection("Account").
                        WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") < 0.1)
            {
                return BadRequest("Not enough cash");
            }
            await db.Collection("Buy").AddAsync(new Dictionary<string, dynamic>() { { "MovieId", vip.Id }, { "User", User.FindFirstValue(ClaimTypes.Sid) }, { "Time", DateTime.UtcNow } });


            await (await db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic> { { "Wallet", (await db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") - 0.1 } });
        }
        else
        {
            if (vip.Choose == 1 || vip.Choose == 3 || vip.Choose == 6 || vip.Choose == 12)
            {
                double minus = new double();
                switch (vip.Choose)
                {
                    case 1:
                        minus = 0.99;
                        break;
                    case 3:
                        minus = 2.79;
                        break;
                    case 6:
                        minus = 5.59;
                        break;
                    case 12:
                        minus = 10.99;
                        break;
                    default:
                        break;
                }
                if ((await db.Collection("Account").
                        WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") < minus)
                {
                    return BadRequest("Not enough cash");
                }
                QuerySnapshot vipCheck = await db.Collection("Vip").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync();
                if (vipCheck.Documents.Count == 0)
                {
                    await db.Collection("Vip").AddAsync(new Dictionary<string, dynamic>() { { "User", User.FindFirstValue(ClaimTypes.Sid) }, { "Time", DateTime.UtcNow.AddMonths(vip.Choose) } });
                    await (await db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).
                        Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic>{{"Wallet", (await db.Collection("Account").
                            WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") - minus} });

                }
                else
                {
                    DateTime dateCheck = vipCheck.Documents[0].GetValue<DateTime>("Time");
                    if (dateCheck < DateTime.UtcNow)
                    {
                        await vipCheck.Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic> { { "Time", DateTime.UtcNow.AddMonths(vip.Choose) } });
                        await (await db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).
                        Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic>{{"Wallet", (await db.Collection("Account").
                            WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") - minus} });
                    }

                    else
                    {
                        await vipCheck.Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic> { { "Time", dateCheck.AddMonths(vip.Choose) } });
                        await (await db.Collection("Account").WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).
                        Documents[0].Reference.UpdateAsync(new Dictionary<string, dynamic>{{"Wallet", (await db.Collection("Account").
                            WhereEqualTo("Id", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync()).Documents[0].GetValue<double>("Wallet") - minus} });
                    }
                }
            }
        }
        return Ok("Success");
    }
    [HttpGet("CanWatch/{Id}")]
    public async Task<ActionResult<bool>> CanWatch(string Id)
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
        QuerySnapshot collectionCheckVip = await db.Collection("Vip").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).OrderByDescending("Time").Limit(1).GetSnapshotAsync();
        
        if (collectionCheckVip.Documents.Count != 0)

        {
            if (collectionCheckVip.Documents[0].GetValue<DateTime>("Time") >= DateTime.UtcNow)
            {
                QuerySnapshot collectionView = await db.Collection("View").WhereEqualTo("Id", Id).WhereEqualTo("Viewer", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync();
                if(collectionView.Documents.Count == 0)
                {
                    await db.Collection("View").AddAsync(new Dictionary<string, dynamic>() { {"Id", Id },{ "Viewer", User.FindFirstValue(ClaimTypes.Sid) },{"Time", DateTime.UtcNow } });
                }
                return await Task.FromResult(true);
            }
            else
            {
                QuerySnapshot collectionCheckBuy = await db.Collection("Buy").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).WhereEqualTo("MovieId", Id).GetSnapshotAsync();
                if (collectionCheckBuy.Documents.Count == 0)
                {
                    return await Task.FromResult(false);
                }
                else
                {
                    QuerySnapshot collectionView = await db.Collection("View").WhereEqualTo("Id", Id).WhereEqualTo("Viewer", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync();
                    if (collectionView.Documents.Count == 0)
                    {
                        await db.Collection("View").AddAsync(new Dictionary<string, dynamic>() { { "Id", Id }, { "Viewer", User.FindFirstValue(ClaimTypes.Sid) }, { "Time", DateTime.UtcNow } });
                    }
                    return await Task.FromResult(true);
                }
            }

        }
        else
        {
            QuerySnapshot collectionCheckBuy = await db.Collection("Buy").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).WhereEqualTo("MovieId", Id).GetSnapshotAsync();
            if (collectionCheckBuy.Documents.Count == 0)
            {
                return await Task.FromResult(false);
            }
            else
            {
                QuerySnapshot collectionView = await db.Collection("View").WhereEqualTo("Id", Id).WhereEqualTo("Viewer", User.FindFirstValue(ClaimTypes.Sid)).GetSnapshotAsync();
                if (collectionView.Documents.Count == 0)
                {
                    await db.Collection("View").AddAsync(new Dictionary<string, dynamic>() { { "Id", Id }, { "Viewer", User.FindFirstValue(ClaimTypes.Sid) }, { "Time", DateTime.UtcNow } });
                }
                return await Task.FromResult(true);
            }
        }
    }
    [HttpGet("Comment/{Id?}/{index:int:min(0)}")]
    public async Task<ActionResult<List<CommentModel>>> Comment(string? Id, int index)
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");

        Query commentSend = db.Collection("Comment").WhereEqualTo("MovieId", Id).OrderByDescending("Time").Offset(index * 5).Limit(5);
        QuerySnapshot commentSnapshot = await commentSend.GetSnapshotAsync();
        List<CommentModel> commentList = new List<CommentModel>();

        
        foreach (DocumentSnapshot item in commentSnapshot.Documents)
        {
            CommentModel commentConvert = item.ConvertTo<CommentModel>();
            int like = (await db.Collection("CommentAcction").WhereEqualTo("CommentId", item.Id).WhereEqualTo("Action", "Like").GetSnapshotAsync()).Documents.Count;
            int Dislike = (await db.Collection("CommentAcction").WhereEqualTo("CommentId", item.Id).WhereEqualTo("Action", "DisLike").GetSnapshotAsync()).Documents.Count;
            commentList.Add(new CommentModel() { Id = commentConvert.Id, Email = commentConvert.Email, MovieId = commentConvert.MovieId, Time = commentConvert.Time, CommentText = censor.CensorText(commentConvert.CommentText), Like = like, DisLike = Dislike });
        }




        return await Task.FromResult(commentList);
    }
    [HttpPost("Acomment")]
    public async Task<ActionResult> Acomment([FromBody] CommentModel comment)
    {
        try
        {
            FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
            comment.Email = User.FindFirstValue(ClaimTypes.Email);
            DocumentReference commentUp = await db.Collection("Comment").AddAsync(comment);
            await commentUp.UpdateAsync(new Dictionary<string, dynamic> { { "Id", commentUp.Id } });

            return Ok("Commented");
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
        
    }
    [HttpPost("Ac/{Id}")]
    public async Task<ActionResult> Ac([FromBody] string ac, string Id)
    {
        FirestoreDb db = FirestoreDb.Create("movie2-e3c7b");
        if ((await db.Collection("CommentAcction").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).WhereEqualTo("CommentId", Id).GetSnapshotAsync()).Documents.Count == 0)
        {
            if (ac == "Like")
            {
                await db.Collection("CommentAcction").AddAsync(new Dictionary<string, dynamic>() { { "Action", "Like" }, { "CommentId", Id }, { "User", User.FindFirstValue(ClaimTypes.Sid) } });
            }
            if (ac == "DisLike")
            {
                await db.Collection("CommentAcction").AddAsync(new Dictionary<string, dynamic>() { { "Action", "DisLike" }, { "CommentId", Id }, { "User", User.FindFirstValue(ClaimTypes.Sid) } });

            }
        }
        else
        {
            QuerySnapshot deleteCommentAction = await db.Collection("CommentAcction").WhereEqualTo("User", User.FindFirstValue(ClaimTypes.Sid)).WhereEqualTo("CommentId", Id).GetSnapshotAsync();
            foreach (DocumentSnapshot item in deleteCommentAction.Documents)
            {

                if (item.GetValue<string>("Action") == ac)
                {
                    await item.Reference.DeleteAsync();
                }
                else
                {
                    if (ac == "Like")
                    {
                        await item.Reference.UpdateAsync(new Dictionary<string, dynamic>() { { "Action", "Like" } });
                    }
                    if (ac == "DisLike")
                    {
                        await item.Reference.UpdateAsync(new Dictionary<string, dynamic>() { { "Action", "DisLike" } });

                    }
                }

            }

        }
        return Ok();
    }
}


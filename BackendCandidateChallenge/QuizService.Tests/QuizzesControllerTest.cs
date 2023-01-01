using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuizService.Model;
using QuizService.Model.Domain;
using QuizService.Services;
using Xunit;

namespace QuizService.Tests;

public class QuizzesControllerTest 
{
    const string QuizApiEndPoint = "/api/quizzes/";

    //private readonly IDbConnection _connection;
    

    //public QuizzesControllerTest(IDbConnection connection)
    //{
    //    _connection = connection;       
    //}

    [Fact]
    public async Task PostNewQuizAddsQuiz()
    {
        var quiz = new QuizCreateModel("Test title");
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(quiz));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"),
                content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }
    }

    [Fact]
    public async Task AQuizExistGetReturnsQuiz()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 1;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var quiz = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal(quizId, quiz.Id);
            Assert.Equal("My first quiz", quiz.Title);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExistGetFails()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
        
    public async Task AQuizDoesNotExists_WhenPostingAQuestion_ReturnsNotFound()
    {
        const string QuizApiEndPoint = "/api/quizzes/999/questions";

        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var question = new QuestionCreateModel("The answer to everything is what?");
            var content = new StringContent(JsonConvert.SerializeObject(question));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"),content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task CreateQuizWithTwoQuestions_VerifyAnswerWithPoints()
    {
         string QuizApiEndPoint = "/api/quizzes";

        //Get the client response after taking the Quiz
       

        using (var testHost = new TestServer(new WebHostBuilder()
                  .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();

            var quiz = new QuizCreateModel("My third Quiz");
            var content = new StringContent(JsonConvert.SerializeObject(quiz));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);

            //Verify the quiz is newly created.
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"));

            //Get the newlyCreatedQuiz to create a questions
            var newlyCreatedQuiz = JsonConvert.DeserializeObject<List<Quiz>>(response.Content.ReadAsStringAsync().Result).OrderByDescending(x => x.Id).First();

            //Create two questions for Quiz
            var qids = await CreateQuestions(testHost, client,  newlyCreatedQuiz);

            //Verify two questions is created
            Assert.True(qids.Count() ==2);

            //Create Answers for the above quiz
            var answerIds = await CreateAnswers(testHost, client,qids.ToList(), newlyCreatedQuiz);

            Assert.True(answerIds.Count() == 6);

            // Create Answers from the client
            var clientAnswerResponseModelList = new List<ClientAnswerResponseModel>()
             { new ClientAnswerResponseModel  { QuizId = newlyCreatedQuiz.Id, QuestionId = qids.ToList()[0], ClientAnswerId = 8 },
               new ClientAnswerResponseModel  { QuizId = newlyCreatedQuiz.Id, QuestionId = qids.ToList()[1], ClientAnswerId = 4 }};


            //Update the correct answers to newly create quiz
            QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}/questions/{qids.ToList()[0]}";
            QuestionUpdateModel questionUpdateModel1 = new QuestionUpdateModel() { CorrectAnswerId = answerIds.ToList()[2]};
            content = new StringContent(JsonConvert.SerializeObject(questionUpdateModel1));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await client.PutAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); //Verify the update operation is succeeded

            QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}/questions/{qids.ToList()[1]}";
            QuestionUpdateModel questionUpdateModel2 = new QuestionUpdateModel() { CorrectAnswerId = answerIds.ToList()[4] };
            content = new StringContent(JsonConvert.SerializeObject(questionUpdateModel2));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await client.PutAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); //Verify the update operation is succeeded

            //Fetch the Quiz
            QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}";

            //Take the Quiz and Provide Response.
            response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"));

            //Give the points to each correct answer.
            int count =0;
            var quizResponseModel=  JsonConvert.DeserializeObject<QuizResponseModel>( await response.Content.ReadAsStringAsync());

            foreach (ClientAnswerResponseModel responseModel in clientAnswerResponseModelList) 
            {                
                    var result = quizResponseModel.Questions.ToList().Find(x => x.Id == responseModel.QuestionId);
                    if(result.CorrectAnswerId == responseModel.ClientAnswerId)
                    {
                        count++;  //Increment for each correct answer.
                    }
            }

            Assert.True(count > 0);  // Assert if there is single correct answer in the quiz.

        }

    }



    //Create the answers for the provided questions
    static async Task<IEnumerable<int>> CreateAnswers(TestServer testHost, HttpClient client, List<int> qids, Quiz newlyCreatedQuiz)
    {
        var firstQuestionanswerList = new List<string>
            {
                "My first Answer for the first Question",
                "My second Answer for the first Question",
                "My third Answer for the first Question"
            };

        var secondQuestionanswerList = new List<string>
            {
                "My first Answer for the second Question",
                "My second Answer for the second Question",
                "My third Answer for the second Question"
            };
       

        string QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}/questions/{qids[0]}/answers";
        List<int> answerids = new List<int>();

        foreach (string str in firstQuestionanswerList)
        {
            var createdQuestion = new QuestionCreateModel(str);
            var content = new StringContent(JsonConvert.SerializeObject(createdQuestion));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            answerids.Add(Convert.ToInt32(response.Headers.Location.ToString().Split('/').Last()));
        }

        QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}/questions/{qids[1]}/answers";

        foreach (string str in secondQuestionanswerList)
        {
            var createdQuestion = new QuestionCreateModel(str);
            var content = new StringContent(JsonConvert.SerializeObject(createdQuestion));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
           var  response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            answerids.Add(Convert.ToInt32(response.Headers.Location.ToString().Split('/').Last()));
        }
        return answerids;
    }

    //Create the questions and get it
    static async Task<IEnumerable<int>> CreateQuestions(TestServer testHost, HttpClient client, Quiz newlyCreatedQuiz)
    {
        var questionList = new List<string>();
        questionList.Add("My first Question for third Quiz");
        questionList.Add("My second Question for third Quiz");

        string QuizApiEndPoint = $"/api/quizzes/{newlyCreatedQuiz.Id}/questions";       
        List<int> qids = new List<int>();

        foreach (string str in questionList)
        {
            var createdQuestion = new QuestionCreateModel(str);
            var content = new StringContent(JsonConvert.SerializeObject(createdQuestion));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var  response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            qids.Add(Convert.ToInt32(response.Headers.Location.ToString().Split('/').Last()));
        }

        return qids;
    }

    //Creating quiz through SQL Statement
    [Fact]
    public void CreateQuizWithSqlQueryTest()
    {
        string QuizApiEndPoint = "/api/quizzes";
        using (var testHost = new TestServer(new WebHostBuilder()
                  .UseStartup<Startup>()))
        {
            var _connection = testHost.Services.GetRequiredService<IDbConnection>();          
            string createQuizSql = "Insert into Quiz(Title) Values('My Third Quiz')";
            var id = _connection.ExecuteScalar(createQuizSql);
            var client = testHost.CreateClient();
            var response = client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"));
            var result = JsonConvert.DeserializeObject<List<Quiz>>(response.Result.Content.ReadAsStringAsync().Result);
            Assert.True(result.Count ==3);
        }
    }

}
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using QuizService.Model;
using QuizService.Services;
using Microsoft.Extensions.Logging;
using QuizService.Exceptions;

namespace QuizService.Controllers;

[Route("api/quizzes")]
public class QuizController : Controller
{
    //To Do: Move the connection property from QuizController to Query Service.
    private readonly IDbConnection _connection;
    private readonly IQueryService _queryService;
    private ILogger<QuizController> _logger;

    public QuizController(IDbConnection connection,IQueryService queryService,ILogger<QuizController> logger)
    {
        _connection = connection;
        _queryService = queryService;
        _logger = logger;
    }

    // GET api/quizzes
    [HttpGet]
    public IEnumerable<QuizResponseModel> Get()
    {
        return _queryService.FetchQuizInfo();

        
    }

    // GET api/quizzes/5
    [HttpGet("{id}")]
    public object Get(int id)
    {
        try
        {
            _logger.LogInformation($"Fetching Quiz for specif Id : {id} ");
            return _queryService.FetchQuizById(id);
        }
        catch (NotFoundException ex )
        {
            _logger.LogError("Getting error while fetching the Quiz by Id ",ex.Message);
            return NotFound();
        }
    }

    

    // POST api/quizzes
    [HttpPost]
    public IActionResult Post([FromBody]QuizCreateModel value)
    {
        object id = _queryService.CreateQuiz(value);
        return Created($"/api/quizzes/{id}", null);
    }

   

    // PUT api/quizzes/5
    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody]QuizUpdateModel value)
    {
        const string sql = "UPDATE Quiz SET Title = @Title WHERE Id = @Id";
        int rowsUpdated = _connection.Execute(sql, new {Id = id, Title = value.Title});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        const string sql = "DELETE FROM Quiz WHERE Id = @Id";
        int rowsDeleted = _connection.Execute(sql, new {Id = id});
        if (rowsDeleted == 0)
            return NotFound();
        return NoContent();
    }

    // POST api/quizzes/5/questions
    [HttpPost]
    [Route("{id}/questions")]
    public IActionResult PostQuestion(int id, [FromBody]QuestionCreateModel value)
    {
        const string sql = "INSERT INTO Question (Text, QuizId) VALUES(@Text, @QuizId); SELECT LAST_INSERT_ROWID();";
        var questionId = _connection.ExecuteScalar(sql, new {Text = value.Text, QuizId = id});
        return Created($"/api/quizzes/{id}/questions/{questionId}", null);
    }

    // PUT api/quizzes/5/questions/6
    [HttpPut("{id}/questions/{qid}")]
    public IActionResult PutQuestion(int id, int qid, [FromBody]QuestionUpdateModel value)
    {
        const string sql = "UPDATE Question SET Text = @Text, CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
        int rowsUpdated = _connection.Execute(sql, new {QuestionId = qid, Text = value.Text, CorrectAnswerId = value.CorrectAnswerId});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6
    [HttpDelete]
    [Route("{id}/questions/{qid}")]
    public IActionResult DeleteQuestion(int id, int qid)
    {
        const string sql = "DELETE FROM Question WHERE Id = @QuestionId";
        _connection.ExecuteScalar(sql, new {QuestionId = qid});
        return NoContent();
    }

    // POST api/quizzes/5/questions/6/answers
    [HttpPost]
    [Route("{id}/questions/{qid}/answers")]
    public IActionResult PostAnswer(int id, int qid, [FromBody]AnswerCreateModel value)
    {
        const string sql = "INSERT INTO Answer (Text, QuestionId) VALUES(@Text, @QuestionId); SELECT LAST_INSERT_ROWID();";
        var answerId = _connection.ExecuteScalar(sql, new {Text = value.Text, QuestionId = qid});
        return Created($"/api/quizzes/{id}/questions/{qid}/answers/{answerId}", null);
    }

    // PUT api/quizzes/5/questions/6/answers/7
    [HttpPut("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult PutAnswer(int id, int qid, int aid, [FromBody]AnswerUpdateModel value)
    {
        const string sql = "UPDATE Answer SET Text = @Text WHERE Id = @AnswerId";
        int rowsUpdated = _connection.Execute(sql, new {AnswerId = qid, Text = value.Text});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6/answers/7
    [HttpDelete]
    [Route("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult DeleteAnswer(int id, int qid, int aid)
    {
        const string sql = "DELETE FROM Answer WHERE Id = @AnswerId";
        _connection.ExecuteScalar(sql, new {AnswerId = aid});
        return NoContent();
    }
}
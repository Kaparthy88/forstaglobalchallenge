using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using QuizService.Model;
using QuizService.Services;
using Microsoft.Extensions.Logging;
using QuizService.Exceptions;

namespace QuizService.Controllers;

//TODO : Add Exceptions for Put,Post and Delete 
[Route("api/quizzes")]
public class QuizController : Controller
{
    private readonly IQueryService _queryService;
    private ILogger<QuizController> _logger;

    public QuizController(IQueryService queryService,ILogger<QuizController> logger)
    {
      
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
        int rowsUpdated =_queryService.UpdateQuizModel(id, value);
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

   

    // DELETE api/quizzes/5
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        int rowsDeleted = _queryService.DeleteQuiz(id);
        if (rowsDeleted == 0)
            return NotFound();
        return NoContent();
    }

    
    // POST api/quizzes/5/questions
    [HttpPost]
    [Route("{id}/questions")]
    public IActionResult PostQuestion(int id, [FromBody]QuestionCreateModel value)
    {
        int questionId = _queryService.CreateNewQuestionsForQuiz(id, value);
        return Created($"/api/quizzes/{id}/questions/{questionId}", null);
    }



    // PUT api/quizzes/5/questions/6
    [HttpPut("{id}/questions/{qid}")]
    public IActionResult PutQuestion(int id, int qid, [FromBody]QuestionUpdateModel value)
    {
        int rowsUpdated = _queryService.UpdateQuestion(qid, value);
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    

    // DELETE api/quizzes/5/questions/6
    [HttpDelete]
    [Route("{id}/questions/{qid}")]
    public IActionResult DeleteQuestion(int id, int qid)
    {
        _queryService.DeleteQuestionById(qid);
        return NoContent();
    }

   

    // POST api/quizzes/5/questions/6/answers
    [HttpPost]
    [Route("{id}/questions/{qid}/answers")]
    public IActionResult PostAnswer(int id, int qid, [FromBody]AnswerCreateModel value)
    {
        int answerId = _queryService.CreateAnswer(qid, value);
        return Created($"/api/quizzes/{id}/questions/{qid}/answers/{answerId}", null);
    }

    // PUT api/quizzes/5/questions/6/answers/7
    [HttpPut("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult PutAnswer(int id, int qid, int aid, [FromBody]AnswerUpdateModel value)
    {
        int rowsUpdated = _queryService.UpdateAnswer(qid, value);
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6/answers/7
    [HttpDelete]
    [Route("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult DeleteAnswer(int id, int qid, int aid)
    {
        _queryService.DeleteAnswer(aid);
        return NoContent();

    }
}
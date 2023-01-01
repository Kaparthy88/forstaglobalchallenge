using Dapper;
using QuizService.Model;
using QuizService.Model.Domain;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using QuizService.Exceptions;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace QuizService.Services
{
    //TODO :Add try, catch blocks to handle Exceptions.    
    public class QueryService : IQueryService
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<QueryService> _logger;

        public QueryService(IDbConnection connection, ILogger<QueryService> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        /// <summary>
        /// Fetches the Quiz Information from Database
        /// </summary>
        /// <returns></returns>
        public IEnumerable<QuizResponseModel> FetchQuizInfo()
        {
            const string sql = "SELECT * FROM Quiz;";
            var quizzes = _connection.Query<Quiz>(sql);
           
            return quizzes.Select(quiz =>
                new QuizResponseModel
                {
                    Id = quiz.Id,
                    Title = quiz.Title
                });
        }

        /// <summary>
        /// Fetch the Quiz based the QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<object> FetchQuizById(int id)
        {
            Quiz quiz = VerifyQuizById(id);

            //Get Questions Based on QuizId
            IEnumerable<Question> questions = await FetchQuestionByQuizId(id);

            //Get Answers based on QuizId
            Dictionary<int, IList<Answer>> answers = await FetchAnswersById(id);

            //Creating the response. 
            return  CreateQuizResponseModel(id, quiz, questions, answers);

        }

        /// <summary>
        /// Verify the quiz based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public Quiz VerifyQuizById(int id)
        {
            try
            {

                const string quizSql = "SELECT * FROM Quiz WHERE Id = @Id;";

                var quiz = _connection.QuerySingle<Quiz>(quizSql, new { Id = id });   // Throws Exception when there is no value or Multiple values

                if (quiz == null)
                    throw new NotFoundException(" Requested Quiz not found");

                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while verifying the Quiz by given ID. Message: {0} ", ex.Message);
                throw new NotFoundException(" Requested Quiz not found"); 
            }
        }

        /// <summary>
        /// Get the answers based on QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Task<Dictionary<int, IList<Answer>>> FetchAnswersById(int id)
        {
            //Verifying the Quiz Id exists or not.
            VerifyQuizById(id);

            const string answersSql = "SELECT a.Id, a.Text, a.QuestionId FROM Answer a INNER JOIN Question q ON a.QuestionId = q.Id WHERE q.QuizId = @QuizId;";
            var answers =   _connection.QueryAsync<Answer>(answersSql, new { QuizId = id }).Result
                .Aggregate(new Dictionary<int, IList<Answer>>(), (dict, answer) =>
                {
                    if (!dict.ContainsKey(answer.QuestionId))
                        dict.Add(answer.QuestionId, new List<Answer>());
                    dict[answer.QuestionId].Add(answer);
                    return dict;
                });
            return Task.FromResult(answers);
        }

        /// <summary>
        /// Get the questions from database on QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Question>> FetchQuestionByQuizId(int id)
        {
            //Verifying the Quiz Id exists or not.
            VerifyQuizById(id);
            const string questionsSql = "SELECT * FROM Question WHERE QuizId = @QuizId;";
            var questions = await _connection.QueryAsync<Question>(questionsSql, new { QuizId = id });
            return questions;
        }


        /// <summary>
        /// Get the questions from database on QuestionId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private  Question FetchQuestionByQuestionId(int qid)
        {    try
            {
                const string questionsSql = "SELECT * FROM Question WHERE Id = @QId;";
                var question =  _connection.QueryAsync<Question>(questionsSql, new { QId = qid });
                return question.Result.FirstOrDefault();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                throw new NotFoundException("Question Id is not present in the database");
            }
        }

        /// <summary>
        /// Create Quiz response for provided Questions and Answers.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quiz"></param>
        /// <param name="questions"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        public QuizResponseModel CreateQuizResponseModel(int id, Quiz quiz, IEnumerable<Question> questions, Dictionary<int, IList<Answer>> answers)
        {
            return new QuizResponseModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Questions = questions.Select(question => new QuizResponseModel.QuestionItem
                {
                    Id = question.Id,
                    Text = question.Text,
                    Answers = answers.ContainsKey(question.Id)
                                    ? answers[question.Id].Select(answer => new QuizResponseModel.AnswerItem
                                    {
                                        Id = answer.Id,
                                        Text = answer.Text
                                    })
                                    : new QuizResponseModel.AnswerItem[0],
                    CorrectAnswerId = question.CorrectAnswerId
                }),
                Links = new Dictionary<string, string>
                {
                    { "self", $"/api/quizzes/{id}" },
                    { "questions", $"/api/quizzes/{id}/questions" }
                }
            };
        }

        /// <summary>
        /// Creating new Quiz
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public object CreateQuiz(QuizCreateModel value)
        {            
            var sql = $"INSERT INTO Quiz (Title) VALUES('{value.Title}'); SELECT LAST_INSERT_ROWID();";
            var id = _connection.ExecuteScalar(sql);
            return id;
        }

        /// <summary>
        /// Update QuizModel based on input parameters
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int UpdateQuizModel(int id, QuizUpdateModel value)
        {
            const string sql = "UPDATE Quiz SET Title = @Title WHERE Id = @Id";
            int rowsUpdated = _connection.Execute(sql, new { Id = id, Title = value.Title });
            return rowsUpdated;
        }

        /// <summary>
        /// Deleted the Quiz by QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int DeleteQuiz(int id)
        {
            VerifyQuizById(id);
            const string sql = "DELETE FROM Quiz WHERE Id = @Id";
            int rowsDeleted = _connection.Execute(sql, new { Id = id });
            return rowsDeleted;
        }

        /// <summary>
        /// Create new questions
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object CreateNewQuestionsForQuiz(int id, QuestionCreateModel value)
        {
            VerifyQuizById(id);
            const string sql = "INSERT INTO Question (Text, QuizId) VALUES(@Text, @QuizId); SELECT LAST_INSERT_ROWID();";
            var questionId  =  _connection.ExecuteScalar(sql, new { Text = value.Text, QuizId = id });
            return questionId;
        }

        /// <summary>
        /// Updating the Question for Quiz
        /// </summary>
        /// <param name="qid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int UpdateQuestion(int qid, QuestionUpdateModel value)
        {
            FetchQuestionByQuestionId(qid);

            string sql;
            int rowsUpdated = 0;

            if (value == null)
            {
                return rowsUpdated;
            }

            if(value.Text== null) 
            {                
                 sql = "UPDATE Question SET  CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
                 rowsUpdated = _connection.Execute(sql, new { QuestionId = qid,  CorrectAnswerId = value.CorrectAnswerId });
                return rowsUpdated;
            }

               sql = "UPDATE Question SET Text = @Text, CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
               rowsUpdated = _connection.Execute(sql, new { QuestionId = qid, Text = value.Text, CorrectAnswerId = value.CorrectAnswerId });
            return rowsUpdated;
        }

        /// <summary>
        /// Delete Question
        /// </summary>
        /// <param name="qid"></param>
        public void DeleteQuestionById(int qid)
        {          
            const string sql = "DELETE FROM Question WHERE Id = @QuestionId";
            _connection.ExecuteScalar(sql, new { QuestionId = qid });
        }

        /// <summary>
        /// Create Anwer based on Input parameters
        /// </summary>
        /// <param name="qid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object CreateAnswer(int qid, AnswerCreateModel value)
        {
            const string sql = "INSERT INTO Answer (Text, QuestionId) VALUES(@Text, @QuestionId); SELECT LAST_INSERT_ROWID();";
            var answerId = _connection.ExecuteScalar(sql, new { Text = value.Text, QuestionId = qid });
            return answerId;
        }

        /// <summary>
        /// Update Answer
        /// </summary>
        /// <param name="qid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int UpdateAnswer(int qid, AnswerUpdateModel value)
        {
            const string sql = "UPDATE Answer SET Text = @Text WHERE Id = @AnswerId";
            int rowsUpdated = _connection.Execute(sql, new { AnswerId = qid, Text = value.Text });
            return rowsUpdated;
        }

        /// <summary>
        /// Delete Answer
        /// </summary>
        /// <param name="aid"></param>
        public void DeleteAnswer(int aid)
        {
            const string sql = "DELETE FROM Answer WHERE Id = @AnswerId";
            _connection.ExecuteScalar(sql, new { AnswerId = aid });
        }
    }
}

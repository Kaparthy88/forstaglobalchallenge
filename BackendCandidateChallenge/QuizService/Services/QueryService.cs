using Dapper;
using Microsoft.AspNetCore.Mvc;
using QuizService.Model;
using QuizService.Model.Domain;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using QuizService.Exceptions;

namespace QuizService.Services
{
    public class QueryService : IQueryService
    {
        private readonly IDbConnection _connection;

        public QueryService(IDbConnection connection) {
            _connection = connection;
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
        public object FetchQuizById(int id)
        {
            const string quizSql = "SELECT * FROM Quiz WHERE Id = @Id;";
            var quiz = _connection.QuerySingle<Quiz>(quizSql, new { Id = id });

            if (quiz == null)
                throw new NotFoundException(" Requested Quiz not found");

            //Get Questions Based on QuizId
            IEnumerable<Question> questions = FetchQuestionByQuizId(id);

            //Get Answers based on QuizId
            Dictionary<int, IList<Answer>> answers = FetchAnswersById(id);

            //Creating the response. 
            return CreateQuizResponseModel(id, quiz, questions, answers);            
            
        }

        /// <summary>
        /// Get the answers based on QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Dictionary<int, IList<Answer>> FetchAnswersById(int id)
        {
            const string answersSql = "SELECT a.Id, a.Text, a.QuestionId FROM Answer a INNER JOIN Question q ON a.QuestionId = q.Id WHERE q.QuizId = @QuizId;";
            var answers = _connection.Query<Answer>(answersSql, new { QuizId = id })
                .Aggregate(new Dictionary<int, IList<Answer>>(), (dict, answer) =>
                {
                    if (!dict.ContainsKey(answer.QuestionId))
                        dict.Add(answer.QuestionId, new List<Answer>());
                    dict[answer.QuestionId].Add(answer);
                    return dict;
                });
            return answers;
        }

        /// <summary>
        /// Get the questions from database on QuizId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private IEnumerable<Question> FetchQuestionByQuizId(int id)
        {
            const string questionsSql = "SELECT * FROM Question WHERE QuizId = @QuizId;";
            var questions = _connection.Query<Question>(questionsSql, new { QuizId = id });
            return questions;
        }

        /// <summary>
        /// Create Quiz response for provided Questions and Answers.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quiz"></param>
        /// <param name="questions"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        private QuizResponseModel CreateQuizResponseModel(int id, Quiz quiz, IEnumerable<Question> questions, Dictionary<int, IList<Answer>> answers)
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
        public int CreateNewQuestionsForQuiz(int id, QuestionCreateModel value)
        {
            const string sql = "INSERT INTO Question (Text, QuizId) VALUES(@Text, @QuizId); SELECT LAST_INSERT_ROWID();";
            var questionId  = (int) _connection.ExecuteScalar(sql, new { Text = value.Text, QuizId = id });
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
            const string sql = "UPDATE Question SET Text = @Text, CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
            int rowsUpdated = _connection.Execute(sql, new { QuestionId = qid, Text = value.Text, CorrectAnswerId = value.CorrectAnswerId });
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
        public int CreateAnswer(int qid, AnswerCreateModel value)
        {
            const string sql = "INSERT INTO Answer (Text, QuestionId) VALUES(@Text, @QuestionId); SELECT LAST_INSERT_ROWID();";
            var answerId = (int)_connection.ExecuteScalar(sql, new { Text = value.Text, QuestionId = qid });
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

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

    }
}

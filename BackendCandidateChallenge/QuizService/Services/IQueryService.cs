using QuizService.Model;
using System.Collections.Generic;

namespace QuizService.Services
{
    public interface IQueryService
    {
        public IEnumerable<QuizResponseModel> FetchQuizInfo();
        public object FetchQuizById(int id);
        public object CreateQuiz(QuizCreateModel value);


    }
}

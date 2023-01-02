using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuizClient.Tests;

namespace QuizClient;

public class QuizClient
{
    private readonly Uri _quizServiceUri;
    private readonly HttpClient _httpClient;

    public QuizClient(Uri quizServiceUri, HttpClient httpClient)
    {
        _quizServiceUri = quizServiceUri;
        _httpClient = httpClient;
    }

    public async Task<Response<IEnumerable<Quiz>>> GetQuizzesAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_quizServiceUri, "/api/quizzes"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.OK ?
            new Response<IEnumerable<Quiz>>(response.StatusCode, await ReadAndDeserializeAsync<IEnumerable<Quiz>>(response)) :
            new Response<IEnumerable<Quiz>>(response.StatusCode, new Quiz[0], await ReadErrorAsync(response));
    }

    public async Task<Response<Quiz>> GetQuizAsync(int id, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_quizServiceUri, "/api/quizzes/" + id));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.OK ?
            new Response<Quiz>(response.StatusCode, await ReadAndDeserializeAsync<Quiz>(response)) :
            new Response<Quiz>(response.StatusCode, Quiz.NotFound, await ReadErrorAsync(response));
    }

    public async Task<Response<Uri>> PostQuizAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        var request =
            new HttpRequestMessage(HttpMethod.Post, new Uri(_quizServiceUri, "/api/quizzes"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(quiz))
            };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.Created ?
            new Response<Uri>(response.StatusCode, response.Headers.Location) :
            new Response<Uri>(response.StatusCode, null, await ReadErrorAsync(response));
    }
		
    public async Task<Response<Uri>> PostAnswerAsync(int quizId, int questionId, Answer answer, CancellationToken cancellationToken)
    {
        var request =
            new HttpRequestMessage(HttpMethod.Post, new Uri(_quizServiceUri, $"/api/quizzes/{quizId}/questions/{questionId}/answers"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(answer))
            };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.Created ?
            new Response<Uri>(response.StatusCode, response.Headers.Location) :
            new Response<Uri>(response.StatusCode, null, await ReadErrorAsync(response));
    }

    public async Task<Response<Uri>> PostQuestionAsync(int quizId, QuizQuestion question, CancellationToken cancellationToken)
    {
        var request =
            new HttpRequestMessage(HttpMethod.Post, new Uri(_quizServiceUri, $"/api/quizzes/{quizId}/questions"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(question))
            };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.Created ?
            new Response<Uri>(response.StatusCode, response.Headers.Location) :
            new Response<Uri>(response.StatusCode, null, await ReadErrorAsync(response));
    }


    public async Task<Response<Uri>> PostQuestionsAsync(int quizId, List<QuizQuestion> questions, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest) ;
       
        foreach (var question in questions)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Post, new Uri(_quizServiceUri, $"/api/quizzes/{quizId}/questions"))
                {
                    Content = new StringContent(JsonConvert.SerializeObject(question))
                };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
             response = await _httpClient.SendAsync(request, cancellationToken);
        }
        return response.StatusCode == HttpStatusCode.Created ?
            new Response<Uri>(response.StatusCode, response.Headers.Location) :
            new Response<Uri>(response.StatusCode, null, await ReadErrorAsync(response));
    }

    public async Task<Response<object>> PutQuestionAsync(int quizId, int questionId, QuizQuestion question, CancellationToken cancellationToken)
    {
        var request =
            new HttpRequestMessage(HttpMethod.Put, new Uri(_quizServiceUri, $"/api/quizzes/{quizId}/questions/{questionId}"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(question))
            };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.NoContent ?
            new Response<object>(response.StatusCode, null) :
            new Response<object>(response.StatusCode, null, await ReadErrorAsync(response));
    }

    public async Task<Response<Uri>> PostQuizResponseAsync(QuestionResponse questionResponse, int quizId)
    {
        var request =
            new HttpRequestMessage(HttpMethod.Post, new Uri(_quizServiceUri, $"/api/quizzes/{quizId}/responses"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(questionResponse))
            };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.SendAsync(request);
        return response.StatusCode == HttpStatusCode.Created ?
            new Response<Uri>(response.StatusCode, response.Headers.Location) :
            new Response<Uri>(response.StatusCode, null, await ReadErrorAsync(response));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        if (response.Content == null)
            return null;
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<T> ReadAndDeserializeAsync<T>(HttpResponseMessage response)
    {
        return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
    }
}

public struct QuizQuestion
{
    public string Text { get; set; }
}

public struct Response<T>
{
    public Response(HttpStatusCode statusCode, T value, string errorMessage = null)
    {
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        Value = value;
    }

    public HttpStatusCode StatusCode { get; }
    public T Value { get; }
    public string ErrorMessage { get; }
}

public class QuizClientException : HttpRequestException
{
    public HttpStatusCode ResponseStatusCode { get; }

    public QuizClientException(HttpStatusCode responseStatusCode)
    {
        ResponseStatusCode = responseStatusCode;
    }
}
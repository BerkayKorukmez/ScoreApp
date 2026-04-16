import http from './http'

export const fetchComments = (matchId) =>
  http.get(`/matchcomment/${matchId}`).then((r) => r.data)

export const postComment = (matchId, content) =>
  http.post('/matchcomment', { matchId, content }).then((r) => r.data)

export const deleteComment = (id) =>
  http.delete(`/matchcomment/${id}`).then((r) => r.data)

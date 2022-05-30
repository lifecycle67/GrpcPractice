# gRPC Practice
examples for practice with grpc-dotnet
## Getting Started
각 프로젝트는 .NET 6.0 / VS2022를 통해 작성되었습니다.
### 프로젝트 정보
- Protos : protobuf 를 작성
- GrpcPracticeClient : WPF client 프로젝트. 
- GrpcPracticeServer : ASP.NET core server 프로젝트
## Greeter
- 단항 호출
- jwt 토큰 인증
- 응답 트레일러 수신
## Streaming
- 서버 스트리밍
- 클라이언트 스트리밍
- 양방향 스트리밍

## Block
- 요청 실패 시 재요청 

## Authenticate
- jwt 토큰 요청
- 인터셉터 구성
- 클라이언트 팩토리 주입

## References
- https://docs.microsoft.com/ko-kr/aspnet/core/grpc/?view=aspnetcore-5.0
- https://docs.microsoft.com/ko-kr/dotnet/architecture/grpc-for-wcf-developers/grpc-overview
- https://github.com/grpc/grpc-dotnet/tree/master/examples
- https://github.com/dotnet-architecture/grpc-for-wcf-developers

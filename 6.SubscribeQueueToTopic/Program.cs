﻿using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


var regionId = Environment.GetEnvironmentVariable("AWS_REGION");
var topicArn = Environment.GetEnvironmentVariable("AWS_TOPIC");

var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(regionId));

var subscribeRequest = new SubscribeRequest
{
    TopicArn = topicArn,
    Protocol = "sqs",
    Endpoint = "arn:aws:sqs:eu-west-1:099229970436:sub3",
};

var response = await snsClient.SubscribeAsync(subscribeRequest);

Console.WriteLine($"Subscribed! Subscription ARN: {response.SubscriptionArn}");
# Estratégia de classificador de padrões Mnist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Origem

A estratégia é uma porta StockSharp do especialista MetaTrader 5 **TestMnistOnnx.mq5** (MQL ID 47225). O script original expõe um ambiente interativo
tela onde o usuário desenha dígitos que são classificados por um modelo MNIST ONNX agrupado. A versão StockSharp mantém o espírito de
reconhecimento de padrões, mas substitui a tela desenhada à mão por uma matriz rolante construída a partir de velas acabadas.

## Conceito

1. Uma janela contínua de `LookbackPeriod` velas concluídas (padrão 28) é tratada como uma grade 28×28 semelhante a uma imagem MNIST.
2. Vários recursos estatísticos – compressão de faixa, força de tendência, impulso, desvio RSI e normalização ATR – são combinados
em uma pontuação sintética de "confiança" que imita a probabilidade da rede neural produzida pelo especialista MQL.
3. Os recursos resultantes são mapeados para uma das dez classes de padrão (`0`–`9`). Cada classe representa um regime de mercado
(flat, tendência, rompimento, retrocesso, reversão, etc.).
4. Quando a classe detectada corresponde ao `TargetClass` selecionado pelo usuário e a confiança sintética está acima de `ConfidenceThreshold`,
a estratégia abre ou reverte uma posição na direção indicada. As posições são achatadas se a classe mudar ou o
a confiança cai abaixo do limite.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `LookbackPeriod` | 28 | Número de velas finalizadas que são convertidas na grade do tipo MNIST. |
| `TargetClass` | 1 | Índice de classe (0–9) que deve desencadear ações de negociação. |
| `ConfidenceThreshold` | 0,6 | Probabilidade sintética mínima que permite o envio de ordens. |
| `Volume` | 1 | Volume de pedidos para novas posições. |
| `CandleType` | Período de 5 minutos | Tipo de dados inscrito para atualizações de velas. |

## Classes de padrões

| Classe | Significado |
|-------|---------|
| 0 | Consolidação plana ou de baixa volatilidade. |
| 1 | Tendência de alta sustentada. |
| 2 | Tendência de baixa sustentada. |
| 3 | Rompimento para cima com forte acompanhamento. |
| 4 | Rompimento para o lado negativo com forte acompanhamento. |
| 5 | Ampla faixa volátil sem viés claro. |
| 6 | Retração de alta dentro de uma tendência de alta. |
| 7 | Retração de baixa dentro de uma tendência de baixa. |
| 8 | Reversão de alta após um declínio prolongado. |
| 9 | Reversão de baixa após um avanço prolongado. |

## Regras de negociação

- Negocia apenas em velas concluídas para permanecer sincronizado com o especialista original que reagiu aos desenhos finalizados.
- Usa ordens de mercado (`BuyMarket`, `SellMarket`) e estabiliza antes de reverter para imitar o comportamento de posição única do
roteiro original.
- O escalonamento de confiança está limitado a `[0, 1]`. Aumentar `ConfidenceThreshold` filtra sinais mais fracos.
- A estratégia não gere paragens de proteção; espera-se que o gerenciamento de riscos seja configurado externamente em StockSharp.

## Dicas de uso

- Selecione um tipo de vela que reflita o ritmo do mercado que você deseja analisar. Prazos mais curtos reagem mais rápido, mas são mais barulhentos.
- Otimize `TargetClass` e `ConfidenceThreshold` juntos – algumas classes são naturalmente mais raras e podem exigir limites mais baixos.
- O classificador de padrões sintéticos é determinístico; não há dependência de bibliotecas externas de tempo de execução ONNX.
- Combine com as ferramentas integradas de proteção contra riscos disponíveis em StockSharp (como `StartProtection`) para controlar a exposição.

## Diferenças do Original

- O desenho interativo e a inferência ONNX são substituídos pela análise de velas totalmente automatizada.
- A “confiança” é uma mistura determinística de indicadores, e não uma probabilidade de rede neural.
- A lógica de negociação é adicionada para converter o reconhecimento de padrões em ordens acionáveis.
- O arquivo de recurso MNIST não é necessário no ambiente StockSharp.

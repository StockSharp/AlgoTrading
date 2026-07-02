# Estratégia de ruptura SR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
A Estratégia de Breakout SR monitora os níveis de suporte e resistência derivados dos canais Donchian em dois períodos de tempo (H1 e H4). Quando uma vela completa fecha acima da resistência ou abaixo do suporte, a estratégia grava uma mensagem de registro informativa. A implementação reflete a lógica de alerta do especialista MQL4 original sem fazer nenhum pedido.

## Como funciona
1. São criadas duas assinaturas de velas: uma para o período de 1 hora e outra para o período de 4 horas.
2. Cada assinatura está vinculada ao seu próprio indicador `DonchianChannels` com um comprimento de lookback configurável (padrão `26`).
3. Uma vez formado o indicador, a estratégia acompanha o fechamento da vela anterior para cada período de tempo.
4. Em cada vela finalizada, o fechamento atual é comparado com as Donchian bandas superior e inferior:
   - Se o fechamento se mover de baixo para cima da banda superior, uma mensagem de “resistência cruzada acima” será registrada.
   - Se o fechamento se mover de cima para baixo da banda inferior, uma mensagem “cruz abaixo do suporte” será registrada.
5. A lógica reproduz o comportamento de notificação do script MQL4 usando entradas `LogInfo` como alertas.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `LookbackLength` | Número de velas usadas para calcular suporte/resistência Donchian. | 26 |
| `Hour1CandleType` | Tipo de vela para assinatura de uma hora. | `TimeFrame(1h)` |
| `Hour4CandleType` | Tipo de vela para assinatura de quatro horas. | `TimeFrame(4h)` |

## Sinais
- **Rompimento H1** – registra quando o fechamento da vela de uma hora cruza acima da resistência ou abaixo do suporte.
- **Rompimento H4** – registra quando o fechamento da vela de quatro horas cruza acima da resistência ou abaixo do suporte.

## Notas
- A estratégia destina-se apenas a alertar; ele não executa negociações.
- Ambas as assinaturas de velas devem fornecer dados altos e baixos para que o indicador Donchian funcione corretamente.
- Ajuste a duração do lookback ou os tipos de velas para corresponder a outras sessões de negociação ou instrumentos.

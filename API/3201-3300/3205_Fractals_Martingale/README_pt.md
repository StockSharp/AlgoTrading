# Estratégia de Fractals Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta pasta contém o port do StockSharp do consultor especialista de MetaTrader "Fractals Martingale". A estratégia combina fractais de Bill Williams, um filtro de tendência baseado em Ichimoku e uma confirmação mensal de MACD. O dimensionamento de posição segue uma sequência martingale clássica que multiplica o volume de operação após cada ciclo perdedor, enquanto um resfriamento opcional evita exposição descontrolada.

## Lógica de trading

1. **Detecção de fractais no período de trabalho** – velas finalizadas são armazenadas em buffer para detectar máximas e mínimas locais separadas por `FractalDepth` vizinhos. Uma configuração de alta é registrada quando a próxima vela abre acima da máxima fractal, enquanto uma configuração de baixa requer a próxima abertura abaixo da mínima fractal. Os níveis detectados permanecem válidos por `FractalLookback` velas processadas.
2. **Filtro de tendência Ichimoku** – o fractal deve se alinhar com a tendência do Ichimoku calculada no período superior definido por `IchimokuCandleType`. Operações compradas exigem Tenkan-sen acima de Kijun-sen; operações vendidas requerem Tenkan-sen abaixo de Kijun-sen.
3. **Confirmação mensal de MACD** – o EA original usava um MACD mensal para decidir se compradores ou vendedores dominam. O port assina a série `MacdCandleType` (velas de 30 dias por padrão) e aceita apenas sinais comprados quando a linha MACD está acima da linha de sinal; sinais vendidos precisam da condição oposta.
4. **Filtro de sessão** – as ordens são colocadas apenas entre `StartHour` (inclusive) e `EndHour` (exclusive). Uma janela de envolvimento é suportada para sessões de trading noturnas.
5. **Escala de volume martingale** – o tamanho base da ordem vem de `TradeVolume`. Após cada rodada perdedora, o próximo volume de ordem é multiplicado por `Multiplier` e alinhado ao passo de volume do instrumento. Operações vencedoras reiniciam a sequência. Quando `MaxConsecutiveLosses` é excedido, o algoritmo pausa por `PauseMinutes` antes de retomar com o volume base.
6. **Troca de direção** – sempre que uma nova operação é enviada, a estratégia compensa automaticamente qualquer posição oposta antes de abrir exposição na direção solicitada.

## Gestão de risco

- `StopLossPips` e `TakeProfitPips` são convertidos para distâncias de preço absolutas usando o tamanho de pip detectado e aplicados via `StartProtection`. Isso espelha o EA original onde ambos os stops eram definidos em pips.
- A implementação original expunha trailing stops opcionais baseados em dinheiro. O port do StockSharp depende do bloco de proteção integrado porque o tratamento real da moeda do portfólio é específico do broker.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `TradeVolume` | Tamanho base da ordem usado para a primeira entrada de uma sequência. |
| `Multiplier` | Fator aplicado ao próximo volume de operação após uma perda. |
| `StopLossPips`, `TakeProfitPips` | Distâncias de stop de proteção e alvo medidas em pips. |
| `FractalDepth` | Número de velas em cada lado necessárias para confirmar um máximo/mínimo fractal. |
| `FractalLookback` | Número máximo de velas processadas para as quais um fractal detectado permanece válido. |
| `StartHour`, `EndHour` | Janela de trading expressa em horas da bolsa. Quando ambos os valores coincidem, o filtro é desabilitado. |
| `MaxConsecutiveLosses` | Número de operações perdedoras antes de a estratégia pausar. |
| `PauseMinutes` | Duração do período de resfriamento ativado após exceder o limite de perda. |
| `TenkanPeriod`, `KijunPeriod`, `SenkouPeriod` | Comprimentos do Ichimoku Kinko Hyo usados no período superior. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Comprimentos de EMA para a confirmação MACD do período superior. |
| `CandleType` | Série de velas primária onde fractais e execuções são avaliados. |
| `IchimokuCandleType` | Período superior usado para calcular as linhas Tenkan e Kijun. |
| `MacdCandleType` | Período usado para calcular o filtro MACD (mensal por padrão). |

## Notas de uso

1. **Cálculo do tamanho do pip** – o valor do pip é derivado de `Security.PriceStep`. Cotações forex de cinco dígitos são automaticamente escaladas para corresponder à definição do MetaTrader usada no EA fonte.
2. **Assinaturas de indicadores** – a estratégia consome até três séries de velas. Certifique-se de que o feed de dados pode fornecer todos os períodos solicitados para manter os filtros sincronizados.
3. **Precauções de martingale** – dobrar o volume aumenta rapidamente a exposição. Use os parâmetros de resfriamento ou reduza o multiplicador se a conta não puder suportar sequências prolongadas de perdas.
4. **Diferenças vs. o EA MT4** – alertas de e-mail/notificação, trailing stops baseados em saldo e verificações de margem explícitas foram removidos porque o StockSharp já lida com conectividade, segurança do portfólio e execução de ordens. A lógica de entrada/saída central corresponde à implementação MQL.

## Arquivos

- `CS/FractalsMartingaleStrategy.cs` – implementação em C# usando a API de estratégia de alto nível.
- `README.md` – documentação em inglês (este arquivo).
- `README_zh.md` – tradução para chinês simplificado.
- `README_ru.md` – tradução para russo.

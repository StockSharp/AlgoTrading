# PosNegDiCrossoverEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **PosNegDiCrossoverStrategy** é uma porta StockSharp do MetaTrader especialista `_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE`. O
o sistema original escuta cruzamentos entre as linhas +DI e -DI do Índice Direcional Médio (ADX) e imediatamente
abre uma posição na direção do novo líder. Cada posição é protegida por stop-loss e take-profit simétricos
limites medidos em pips, e negociações perdidas desencadeiam um ciclo de recuperação estilo martingale que entra novamente com um volume multiplicado
até que um número fixo de tentativas seja alcançado ou ocorra uma saída lucrativa.

## Lógica de negociação
1. **Detecção de sinal** – quando a vela finalizada fornece novos valores ADX, a estratégia compara o +DI e -DI atuais
leituras com as anteriores. Um sinal de alta aparece quando +DI cruza acima de -DI, enquanto um sinal de baixa é gerado quando
+DI cruza abaixo de -DI. Apenas uma entrada inicial por barra é permitida para espelhar a proteção MQL que evitou negociações duplicadas em
a mesma vela.
2. **Filtro de tempo** – as entradas são permitidas apenas dentro de uma janela diária definida pelo usuário. Fora da janela a estratégia continua gerenciando
posições ativas (stops virtuais e take-profit), mas não abre novos ciclos nem continua uma sequência de martingale.
3. **Colocação de ordem** – uma ordem de mercado é enviada na direção detectada com o volume base configurado. Depois de preencher o
estratégia converte `TakeProfitPips` e `StopLossPips` em preços absolutos usando a etapa do instrumento (um multiplicador de 10x é
aplicado para instrumentos cotados com 3 ou 5 casas decimais) e armazena esses níveis para verificações manuais de saída.
4. **Manuseio da proteção** – cada vela acabada é inspecionada: uma posição longa é fechada se a baixa perfurar o stop ou se o
alto atinge o alvo; as posições curtas utilizam as condições simétricas. As saídas são executadas com ordens de mercado para que o ciclo
pode avaliar o resultado antes de decidir o próximo passo.
5. **Martingale loop** – após uma perda a estratégia multiplica o volume atual por `MartingaleMultiplier`, incrementa o ciclo
contador e entra imediatamente novamente na mesma direção (respeitando a janela de negociação). Quando ocorre uma saída lucrativa ou o
número de tentativas atinge `MartingaleCycleLimit`, o ciclo é redefinido para o volume base e aguarda o próximo cruzamento ADX.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | Período de 15 minutos | Série de velas usada para cálculos ADX e monitoramento de parada/alvo. |
| `AdxPeriod` | 14 | Comprimento do indicador Índice Direcional Médio. |
| `UseTimeFilter` | `true` | Ativa a janela de negociação diária. |
| `StartTime` | 00:00 | Início do pregão (horário de câmbio). |
| `StopTime` | 23:59 | Fim do pregão (horário de câmbio). |
| `OrderVolume` | 0,1 | Volume inicial de ordens de mercado para cada ciclo. |
| `TakeProfitPips` | 10 | Distância até a meta de lucro em pips (convertida em preço usando a etapa do instrumento). |
| `StopLossPips` | 10 | Distância até o batente de proteção em pips. |
| `MartingaleMultiplier` | 2 | Multiplicador de volume aplicado após cada negociação perdida durante o ciclo martingale. |
| `MartingaleCycleLimit` | 5 | Número máximo de reentradas martingale permitidas para o mesmo sinal. |

## Notas
- A estratégia verifica `IsFormedAndOnlineAndAllowTrading()` antes de enviar qualquer pedido, garantindo inicialização adequada e risco
controles da estrutura.
- O tratamento virtual de stop-loss e take-profit imita o comportamento MetaTrader em que as ordens de proteção são anexadas diretamente ao
posição. Eles são avaliados em velas finalizadas para permanecerem compatíveis com o StockSharp API de alto nível.
- Quando a janela de negociação está desativada (seja por parâmetro ou pela definição de horários de início e término idênticos), a estratégia se comporta como
um sistema 24 horas por dia, 5 dias por semana, idêntico ao especialista original com `is_start` e `is_stop` cobrindo o dia inteiro.

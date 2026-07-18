# BrakeoutTraderV1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

BrakeoutTraderV1 é um sistema de rompimento simples construído em torno de um nível de preço estático. A estratégia observa os preços de fechamento de velas completas e entra quando o mercado fecha através do nível de rompimento escolhido. Quando o fechamento cruza acima do nível, uma posição comprada é aberta (sujeita a filtros de direção); quando cruza abaixo, uma posição vendida é assumida. O tamanho da posição é calculado a partir do percentual de risco configurado e da distância ao stop-loss, habilitando escalonamento automático com o patrimônio da conta.

## Lógica de trading
- Processar apenas velas finalizadas do `CandleType` selecionado. Velas incompletas são ignoradas.
- Manter o último preço de fechamento para detectar rompimentos do `BreakoutLevel` especificado pelo usuário.
- **Entrada comprada**: a última vela fecha acima de `BreakoutLevel` enquanto o fechamento anterior estava em ou abaixo do nível, e `EnableLong` é verdadeiro. Qualquer posição vendida aberta é zerada antes de submeter a nova ordem.
- **Entrada vendida**: a última vela fecha abaixo de `BreakoutLevel` enquanto o fechamento anterior estava em ou acima do nível, e `EnableShort` é verdadeiro. Qualquer posição comprada é fechada primeiro.
- As ordens são enviadas a mercado. A quantidade é calculada para que a perda entre o preço de entrada e a distância ao stop-loss corresponda ao `RiskPercent` do patrimônio atual da conta. Se o tamanho baseado em risco não puder ser determinado, a estratégia recorre ao valor base `Volume`.
- Após cada entrada, a estratégia armazena níveis estáticos de take-profit e stop-loss expressos em pips (`StopLossPoints` e `TakeProfitPoints`). Quando o preço atinge qualquer nível, a posição aberta é fechada a mercado e os níveis em cache são limpos.
- Nunca há múltiplas operações abertas na mesma direção simultaneamente porque a posição líquida é gerenciada explicitamente.

## Gestão de posição
- Um stop protetor é definido abaixo da entrada para operações compradas e acima da entrada para vendidas. A distância é `StopLossPoints * pip`, onde pip é derivado de `Security.PriceStep` e sua precisão (3 ou 5 casas decimais implicam um ajuste de dez vezes, como na implementação MQL original).
- Um alvo de lucro é definido simetricamente usando `TakeProfitPoints`.
- Se stop e alvo seriam acionados durante a mesma vela, o stop é avaliado primeiro, refletindo execução conservadora do servidor.
- Sinais opostos sempre fecham qualquer posição ativa antes de estabelecer a nova, evitando exposição hedgeada.
- O helper redefine automaticamente os níveis em cache quando a posição retorna a zero.

## Parâmetros
- `BreakoutLevel` – Nível de preço estático monitorado para rompimentos.
- `EnableLong` / `EnableShort` – Filtros de direção que permitem abrir posições compradas ou vendidas.
- `StopLossPoints` – Distância do stop-loss em pips (múltiplos do tamanho de pip derivado).
- `TakeProfitPoints` – Distância do take-profit em pips.
- `RiskPercent` – Percentual do patrimônio da conta a arriscar por operação. Usado para determinar o volume da ordem a partir da distância do stop-loss.
- `CandleType` – Série de dados de vela usada para geração de sinais (padrão: velas de 15 minutos).
- `Volume` – Tamanho base da ordem usado quando o cálculo baseado em risco não está disponível.

## Detalhes
- **Critérios de entrada**: O fechamento cruza acima/abaixo de `BreakoutLevel` na última vela completa.
- **Comprado/Vendido**: Opera ambas as direções, controlado pelos indicadores `EnableLong` e `EnableShort`.
- **Critérios de saída**: Níveis estáticos de stop-loss e take-profit, mais zerar na ocorrência de sinais de rompimento opostos.
- **Stops**: Stop-loss de distância fixa medida em pips.
- **Valores padrão**: `BreakoutLevel = 0`, `StopLossPoints = 140`, `TakeProfitPoints = 180`, `RiskPercent = 10`, `CandleType = 15 minutos`, `EnableLong = EnableShort = true`.
- **Filtros**: Nenhum além dos seletores de direção; nenhum filtro de tendência ou volatilidade é aplicado.

## Dicas de uso
- O instrumento deve suportar o cálculo de pip utilizado pelo EA original. Para símbolos com 3 ou 5 casas decimais, o pip é automaticamente escalado por dez.
- Garantir que o portfólio conectado forneça `CurrentValue` para que o dimensionamento baseado em risco funcione corretamente. Se o patrimônio não estiver disponível, as operações serão executadas com o `Volume` configurado.
- Como as ordens são executadas a mercado, os preenchimentos reais podem diferir do fechamento da vela. Ajustar as distâncias de stop e take para levar em conta o slippage se necessário.

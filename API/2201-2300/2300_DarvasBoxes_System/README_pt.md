# Estratégia do Sistema de Caixas Darvas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia implementa uma abordagem de rompimento baseada no conceito clássico de **Darvas Boxes**. Ela monitora o movimento do preço dentro de um intervalo dinâmico (caixa) calculado usando o indicador **Donchian Channels**. Quando o preço fecha acima do limite superior da caixa, uma posição comprada é aberta. Quando o preço fecha abaixo do limite inferior, uma posição vendida é aberta. Níveis opcionais de stop-loss e take-profit fornecem gerenciamento básico de risco.

## Como funciona

1. Para cada candle, o indicador Donchian Channels calcula os limites superior e inferior usando o `BoxPeriod` especificado.
2. A estratégia rastreia os valores anteriores dos limites para detectar rompimentos.
3. Se o preço de fechamento atual cruzar acima do limite superior anterior, a estratégia:
   - Fecha qualquer posição vendida existente (se permitido).
   - Abre uma nova posição comprada (se permitido).
4. Se o preço de fechamento atual cruzar abaixo do limite inferior anterior, a estratégia:
   - Fecha qualquer posição comprada existente (se permitido).
   - Abre uma nova posição vendida (se permitido).
5. As posições ativas são monitoradas para verificar as condições de stop-loss e take-profit.

## Parâmetros

- **BoxPeriod** (`int`): Número de candles usados para construir a caixa de preço. O valor padrão é 20.
- **StopLoss** (`decimal`): Distância do preço de entrada até o nível de stop-loss. O valor padrão é 1000.
- **TakeProfit** (`decimal`): Distância do preço de entrada até o nível de take-profit. O valor padrão é 2000.
- **AllowBuyEntry** (`bool`): Habilita a abertura de posições compradas. O valor padrão é `true`.
- **AllowSellEntry** (`bool`): Habilita a abertura de posições vendidas. O valor padrão é `true`.
- **AllowBuyExit** (`bool`): Habilita o fechamento de posições compradas em sinais inversos ou eventos de risco. O valor padrão é `true`.
- **AllowSellExit** (`bool`): Habilita o fechamento de posições vendidas em sinais inversos ou eventos de risco. O valor padrão é `true`.
- **CandleType** (`DataType`): Tipo de candles usados para cálculos. O valor padrão são candles de 4 horas.

## Uso

1. Anexe a estratégia a um ativo e defina os valores de parâmetros desejados.
2. Inicie a estratégia. Ela irá subscrever a série de candles configurada e processar os dados recebidos.
3. As operações são executadas com ordens a mercado quando as condições de rompimento são atendidas.
4. Níveis opcionais de stop-loss e take-profit gerenciam posições abertas.

## Notas

- A estratégia usa a API de alto nível com `BindEx` para conectar os valores do indicador e os dados de candles.
- Coleções internas são evitadas; os valores do indicador são acessados através do callback de vinculação.
- Apenas candles concluídos são processados para garantir sinais confiáveis.
- Os comentários dentro do código estão em inglês, conforme exigido.

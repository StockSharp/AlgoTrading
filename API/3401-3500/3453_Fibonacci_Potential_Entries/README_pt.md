# Fibonacci Estratégia de entradas potenciais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz o comportamento do consultor especialista **EA_PUB_FibonacciPotentialEntries** original. Ele coloca dois pedidos de limite nos níveis de retração Fibonacci de 50% e 61% e gerencia seu ciclo de vida usando o StockSharp API de alto nível.

## Lógica de negociação

1. **Colocação inicial**
   - Assim que as cotações de compra/venda estiverem disponíveis, a estratégia calcula o spread atual e envia duas ordens de limite:
     - Ordem nº 1: colocada no nível de 50% com um stop de proteção abaixo (ou acima para posições vendidas) do nível de 61%.
     - Ordem #2: colocada no nível 61% com um stop de proteção colocado a meio caminho do nível 100%.
   - Os volumes são dimensionados de modo que a primeira negociação arrisque 0,7% da carteira e a segunda negociação arrisque a parte restante do parâmetro `RiskPercent`.

2. **Manuseio de alvo**
   - Quando o preço atinge o nível `TargetPrice` a estratégia fecha metade de cada posição preenchida usando ordens de mercado.
   - Após a saída parcial, o volume restante fica protegido no ponto de equilíbrio (preço de entrada). Se o mercado retornar a esse nível, o restante da posição será fechado automaticamente.

3. **Direção**
   - `IsBullish = true` cria limites de compra (modelo otimista original).
   - `IsBullish = false` reflete o comportamento com limites de venda e verificações de stop/target invertidas.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `PriceOn50Level` | Nível de preço para o primeiro pedido com limite. |
| `PriceOn61Level` | Nível de preço para o segundo pedido com limite. |
| `PriceOn100Level` | Nível de referência utilizado para calcular o segundo stop comercial. |
| `TargetPrice` | Meta de lucro compartilhada para ambas as posições. |
| `RiskPercent` | Percentagem total do capital da carteira arriscada em ambas as entradas. |
| `IsBullish` | Escolhe entre configurações longas e curtas. |

## Notas de conversão

- Somente auxiliares de alto nível (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyMarket`, `SellMarket`) são usados, exatamente conforme exigido pelas diretrizes do repositório.
- Saídas parciais e ajustes de ponto de equilíbrio são reproduzidos com ordens de mercado, correspondendo ao comportamento do robô MQL sem depender de chamadas de modificação de ordem de baixo nível.
- Os volumes de posição são normalizados para a etapa de volume do instrumento para permanecerem consistentes com as convenções StockSharp.

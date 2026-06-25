# Estratégia Multi Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Multi Stochastic é uma implementação de alto nível do StockSharp do consultor especializado MetaTrader 5 "Multi Stochastic (barabashkakvn's edition)". Monitora até quatro pares de moedas simultaneamente e depende de sinais sincronizados das leituras do Oscilador Stochastic (5, 3, 3). A estratégia abre uma única posição de mercado por símbolo quando ocorre um cruzamento de sobrecompra ou sobrevenda e fecha as operações através de alvos fixos de stop-loss e take-profit baseados em pips.

## Lógica de trading
- Cada símbolo configurado recebe seu próprio Oscilador Stochastic (comprimento 5, suavização %K 3, suavização %D 3).
- Um sinal comprado é produzido quando o %K atual está abaixo do OversoldLevel (padrão 20), a barra anterior tinha %K abaixo de %D, e a barra atual fecha com %K cruzando acima de %D.
- Um sinal vendido é produzido quando o %K atual está acima do OverboughtLevel (padrão 80), a barra anterior tinha %K acima de %D, e a barra atual fecha com %K cruzando abaixo de %D.
- Apenas uma posição aberta por instrumento é permitida. Sinais adicionais são ignorados até que a posição existente esteja fechada.

## Gestão de risco
- Os valores de stop-loss e take-profit são expressos em pips. A estratégia converte automaticamente pips em distâncias de preço absolutas multiplicando pelo passo de preço do instrumento e ajustando para cotações forex de 3 ou 5 dígitos (pip = passo × 10 para esses instrumentos).
- Posições compradas fecham quando o mínimo da vela toca o nível de stop-loss ou o máximo da vela alcança o nível de take-profit.
- Posições vendidas fecham quando o máximo da vela toca o nível de stop-loss ou o mínimo da vela alcança o nível de take-profit.

## Parâmetros
- `CandleType` – período usado para todas as velas inscritas (padrão: 1 hora).
- `StochasticLength` – comprimento base do Oscilador Stochastic (padrão: 5).
- `StochasticKPeriod` – período de suavização para %K (padrão: 3).
- `StochasticDPeriod` – período de suavização para %D (padrão: 3).
- `OversoldLevel` – limiar usado para detectar condições de sobrevenda (padrão: 20).
- `OverboughtLevel` – limiar usado para detectar condições de sobrecompra (padrão: 80).
- `StopLossPips` – distância ao stop protetor em pips (padrão: 50).
- `TakeProfitPips` – distância ao alvo de lucro em pips (padrão: 10).
- `UseSymbol1` … `UseSymbol4` – habilita o trading para o respectivo slot de símbolo (padrão: true).
- `Symbol1` … `Symbol4` – instrumentos negociados por cada slot. Symbol 1 recorre ao instrumento principal da estratégia quando não especificado.

## Notas de implementação
- Cada assinatura de símbolo é independente. Cada uma usa `SubscribeCandles` com `BindEx` para receber atualizações de `StochasticOscillatorValue` junto com dados de velas.
- Os valores anteriores de %K e %D são armazenados em cache por símbolo para emular a lógica de detecção de cruzamento do MT5.
- Os parâmetros de risco são recalculados para cada entrada, e os níveis de stop/take são reiniciados após o fechamento de uma posição ou quando nenhuma posição existe.
- As ordens são enviadas com `BuyMarket`/`SellMarket` usando a propriedade `Volume` compartilhada, correspondendo à restrição de posição única do especialista original.

## Diferenças da versão MT5
- A versão StockSharp utiliza assinaturas de alto nível em vez de chamadas manuais de atualização de taxas.
- A detecção do tamanho do pip baseia-se em `Security.PriceStep` e `Security.Decimals`. Se os metadados não estiverem disponíveis, os stops e alvos permanecem desabilitados para evitar cálculos de risco incorretos.
- Os hooks de registro e desenho de gráficos estão prontos para extensão, mas não são necessários para o comportamento principal.

## Dicas de uso
1. Atribua os instrumentos desejados aos slots de símbolos e ajuste o período de velas para corresponder ao seu horizonte de trading.
2. Certifique-se de que as distâncias de stop-loss e take-profit são compatíveis com o tamanho de tick do instrumento para evitar fechamentos imediatos.
3. Desabilite os slots de símbolos não utilizados para reduzir o consumo de recursos ao monitorar menos instrumentos.

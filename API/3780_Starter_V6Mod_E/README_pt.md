# Iniciante V6 Mod E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

**Starter V6 Mod E** é uma conversão StockSharp de alto nível do MetaTrader 4 consultor especialista `Starter_v6mod_e_www_forex-instruments_info.mq4`. A porta mantém a combinação original de extremos do oscilador Laguerre, confirmação de impulso duplo EMA, filtragem CCI e controle de ângulo EMA enquanto adapta a execução à arquitetura orientada a eventos de StockSharp.

## Lógica de negociação

- **Gate de tendência:** uma inclinação EMA de 34 períodos é medida entre turnos de início/fim configuráveis. A inclinação é expressa em unidades pip; apenas inclinações positivas permitem negociações longas, apenas inclinações negativas permitem vendas e leituras planas bloqueiam novas entradas.
- **Extremos de Laguerre:** um Laguerre RSI feito à mão (gama = 0,7 por padrão) rastreia estados de sobrevenda/sobrecompra na escala de 0–1. Os longos exigem que os valores atuais e anteriores permaneçam abaixo do nível `Laguerre Oversold`, os vendidos exigem ambos os valores acima de `Laguerre Overbought`.
- **EMA filtro de impulso:** EMAs de 120 e 40 períodos (preço médio) devem subir para posições compradas e cair para posições vendidas, refletindo o filtro MA original.
- **CCI confirmação:** um CCI de 14 períodos deve estar abaixo de `-CCI Threshold` para posições longas e acima de `+CCI Threshold` para posições curtas, replicando o filtro `Alpha` de MQL.
- **Segurança de sexta-feira:** novas negociações são bloqueadas após `Friday Block Hour` e quaisquer posições restantes são liquidadas quando `Friday Exit Hour` for alcançado.

## Gestão de risco

- Distâncias configuráveis de stop-loss, take-profit e trailing-stop (em pips) emulam o bloco de gerenciamento de dinheiro do especialista.
- Os trailing stops seguem o melhor preço favorável após a entrada e fecham a negociação quando a retração excede a distância configurada.
- O fechamento manual da posição é executado por meio de `SellMarket`/`BuyMarket`, garantindo conformidade de alto nível com API.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume de pedidos para cada entrada no mercado. |
| `StopLossPips` | Distância de parada protetora em pips. |
| `TakeProfitPips` | Meta de lucro em pips. |
| `TrailingStopPips` | Distância do trailing stop em pips (0 desativa o trailing). |
| `SlowEmaPeriod` | Período de lentidão EMA calculado em PRICE_MEDIAN. |
| `FastEmaPeriod` | Período do EMA rápida calculado em PRICE_MEDIAN. |
| `AngleEmaPeriod` | Período EMA usado para o detector de ângulo. |
| `AngleStartShift` / `AngleEndShift` | Mudanças de barra usadas para calcular a inclinação EMA. |
| `AngleThreshold` | Inclinação mínima (em unidades pip) necessária para permitir a negociação. |
| `CciPeriod` / `CciThreshold` | Período e limite absoluto para o filtro CCI. |
| `LaguerreGamma` | Parâmetro gama para o oscilador Laguerre. |
| `LaguerreOversold` / `LaguerreOverbought` | Limiares de entrada na escala de Laguerre de 0–1. |
| `CandleType` | Tipo de dados Candle (padrão 1 minuto). |
| `FridayBlockHour` / `FridayExitHour` | Horas (horário local do instrumento) controlando os limites de risco de sexta-feira. |

## Notas de conversão

- O oscilador Laguerre é implementado diretamente a partir da fórmula recursiva original, mantendo a faixa de saída 0–1 e suavização gama.
- A inclinação EMA substitui o auxiliar de ângulo MQL calculando diferenças normalizadas por pip entre pontos históricos EMA.
- Recursos de gerenciamento de dinheiro, como corte de patrimônio e empilhamento de grade, são omitidos intencionalmente porque a variante MT4 sendo convertida os desativou por padrão e StockSharp incentiva o controle explícito do portfólio.
- Os pedidos são enviados por meio de `BuyMarket`/`SellMarket` e dependem de `OnNewMyTrade` para rastrear preços de preenchimento para lógica de rastreamento.

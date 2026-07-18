# Estratégia MasterMind 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
MasterMind 2 é uma conversão do consultor especialista "TheMasterMind2" MQL4. A estratégia aguarda valores extremos nos indicadores Stochastic Oscillator e Williams %R para detectar pontos de exaustão. Quando ambos os indicadores mostram condições extremas de sobrevenda, abre uma posição longa, e quando ambos mostram condições extremas de sobrecompra, abre uma posição curta. A lógica opera apenas em velas totalmente fechadas, imitando o comportamento original do Expert Advisor.

## Indicadores
- **Stochastic Oscilador** – configurado com um longo lookback para avaliar os níveis de sobrecompra e sobrevenda. A linha de sinal %D é comparada com os limites.
- **Williams %R** – confirma a força do extremo exigindo leituras próximas de -100 para posições compradas e próximas de 0 para posições vendidas.

## Regras de entrada
1. Espere uma vela fechar.
2. Calcule o oscilador Stochastic e obtenha o valor do sinal% D.
3. Calcule Williams %R sobre o lookback configurado.
4. **Entrada longa**: se `%D < 3` e `Williams %R < -99.9`, feche qualquer exposição curta existente e compre.
5. **Entrada curta**: se `%D > 97` e `Williams %R > -0.1`, feche qualquer exposição longa existente e venda.

## Regras de saída
- Os níveis de stop loss e takeprofit são aplicados em relação ao preço de entrada usando distâncias de pontos configuráveis.
- O trailing stop pode estreitar o stop de proteção quando o preço se mover favoravelmente na etapa especificada.
- Uma opção de ponto de equilíbrio move o stop loss para o nível de entrada depois que a negociação acumula a distância de lucro necessária.
- Os sinais opostos fecham imediatamente a posição atual antes de abrir uma nova.

## Parâmetros
- `Trade Volume` – volume de contrato enviado com cada ordem de mercado.
- `Stochastic Period`, `Stochastic %K`, `Stochastic %D` – parâmetros do oscilador Stochastic.
- `Williams %R Period` – período de lookback para o cálculo de Williams %R.
- `Stop Loss`, `Take Profit` – distâncias de proteção em faixas de preço.
- `Trailing Stop`, `Trailing Step` – controla o gerenciamento de parada dinâmica.
- `Break Even` – distância em pontos necessária para garantir o preço de entrada.
- `Candle Type` – período de tempo ou tipo de vela personalizado usado nos cálculos.

## Notas
- A estratégia depende exclusivamente de velas finalizadas, correspondendo à implementação original MQL4.
- Todas as ordens são emitidas a mercado com volume definido por `Trade Volume`.
- Habilite ou desabilite os recursos de proteção definindo os parâmetros de distância como zero.

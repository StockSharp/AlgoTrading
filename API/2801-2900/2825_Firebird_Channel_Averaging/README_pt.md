# Estratégia de Averaging de Canal Firebird
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia de Averaging de Canal Firebird replica o expert "Firebird v0.60" do MetaTrader 5 usando a API de alto nível do StockSharp. Opera em um canal de média móvel configurável e faz averaging progressivamente nas posições quando o preço se distancia do canal. A abordagem é projetada para trading forex de reversão à média onde são necessárias entradas estilo grid e controles de risco baseados em pips.

## Configuração de Indicadores
- Uma média móvel (simples, exponencial, suavizada ou ponderada) é calculada na série de velas selecionada. A fonte de preço (fechamento, máxima, mínima, mediana, etc.) pode ser configurada.
- As bandas superiores e inferiores do canal são derivadas deslocando a média móvel por uma porcentagem definida pelo usuário.

## Lógica de Entrada
1. **Condições de Compra**
   - O preço da fonte de vela escolhida fecha abaixo da banda inferior.
   - Não existe posição, ou a nova entrada está pelo menos `Step (pips)` distante do último preenchimento ao considerar o crescimento de `Step Exponent`.
   - A estratégia impõe um período de espera de dois intervalos de vela entre entradas.
2. **Condições de Venda**
   - O preço fecha acima da banda superior.
   - Verificações de distância e período de espera idênticas à lógica longa devem ser satisfeitas.

Quando ocorre um sinal válido, a estratégia envia uma ordem de mercado com o volume de lotes configurado. Apenas uma direção é mantida de cada vez — sinais opostos aguardarão até que o inventário atual seja fechado pelas regras de risco.

## Gestão de Posições
- Cada entrada é armazenada para que a estratégia possa calcular o preço médio do grid aberto.
- Os níveis de stop loss e take profit são definidos em pips. Para uma posição única, o stop loss equivale ao preço de entrada menos/mais `Stop Loss (pips)` e o take profit equivale ao preço de entrada mais/menos `Take Profit (pips)`.
- Quando múltiplas posições existem, a distância do stop loss é dividida pelo número de entradas, emulando o comportamento de averaging do expert original.
- Os objetivos de lucro permanecem fixos em relação ao preço médio, enquanto as saídas de stop loss são recalculadas em cada vela.
- O trading pode ser opcionalmente desabilitado nas sextas-feiras.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Tamanho da ordem em lotes para cada entrada com averaging (padrão 0.1). |
| `Stop Loss (pips)` | Distância do stop protetor em pips (padrão 50). |
| `Take Profit (pips)` | Distância do take profit em pips (padrão 150). |
| `MA Period` | Comprimento de lookback da média móvel (padrão 10). |
| `MA Shift` | Deslocamento adiantado em velas aplicado à saída da média móvel. |
| `MA Type` | Método de cálculo da média móvel: Simple, Exponential, Smoothed ou Weighted. |
| `Price Source` | Preço de vela usado para cálculos do indicador (padrão fechamento). |
| `Channel %` | Deslocamento percentual da média móvel usado para formar as bandas (padrão 0.3%). |
| `Trade Friday` | Habilita ou desabilita o trading nas sextas-feiras. |
| `Step (pips)` | Distância mínima em pips entre ordens com averaging (padrão 30). |
| `Step Exponent` | Expoente que escala o passo com base no número de entradas abertas (0 mantém o passo constante). |
| `Candle Type` | Período para as velas de trabalho. |

## Notas
- A estratégia assume que o `PriceStep` do instrumento representa um pip. Se não disponível, recorre a 0.0001.
- As saídas protetoras são executadas com ordens de mercado em vez de ordens nativas de stop/limit para manter consistência com a API de alto nível.
- O grid de averaging é limitado pela lógica de período de espera e pela distância crescente quando um expoente de passo maior que zero é usado.

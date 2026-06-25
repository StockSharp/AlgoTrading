# Estratégia de Zonas RSI de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de rompimento multi-banda de Bollinger Bands convertido do expert MetaTrader «Bollinger Bands RSI». A estratégia deriva três envelopes de Bollinger com períodos idênticos mas desvios diferentes para criar bandas «amarela», «azul» e «vermelha». Ordens são acionadas quando o preço revisita zonas configuráveis ao redor dessas bandas, com confirmação opcional por filtros RSI e Estocástico.

## Lógica da estratégia
- A banda principal (amarela) usa o multiplicador de desvio configurado.
- A banda azul reduz pela metade o desvio, criando um envelope mais estreito.
- A banda vermelha dobra o desvio, produzindo um envelope externo amplo.
- Os valores de RSI e Estocástico são avaliados na vela finalizada anterior (`Bar Shift`) para corresponder ao comportamento original do EA.
- `Only One Position` controla se novas ordens são permitidas apenas quando a posição líquida está zerada ou se operações de escala adicionais são permitidas quando o preço retorna à linha média de Bollinger.

## Critérios de entrada
### Entradas compradas
1. O preço na vela atual cai para ou abaixo da zona de entrada comprada selecionada (`Entry Mode`):
   - Ponto médio entre amarela e azul, azul e vermelha, ou uma das bandas individuais.
2. Confirmações opcionais:
   - Filtro RSI: RSI ≤ `100 - RSI Lower`.
   - Filtro Estocástico: %K < `100 - Stochastic Lower`.
3. Pré-requisitos de posição:
   - Se `Only One Position` estiver habilitado, a posição líquida deve estar zerada.
   - Caso contrário, ordens compradas adicionais são bloqueadas até que a vela feche acima da banda média (amarela), emulando a lógica de bloqueio do EA.

### Entradas vendidas
1. O preço na vela atual sobe para ou acima da zona de entrada vendida selecionada (espelha as opções compradas).
2. Confirmações opcionais:
   - Filtro RSI: RSI ≥ `RSI Lower`.
   - Filtro Estocástico: %K > `Stochastic Lower`.
3. Os pré-requisitos de posição espelham a lógica comprada (posição zerada para modo de operação única ou estado desbloqueado quando a vela fecha de volta abaixo da banda média).

## Critérios de saída
- O modo de fechamento é determinado por `Closure Mode`:
  - `Middle Line`: sair de posições compradas quando o preço atinge a banda média de Bollinger; sair de vendidas quando o preço a toca por cima.
  - `Between Yellow and Blue` / `Between Blue and Red`: sair nos mesmos pontos médios usados para entradas; padrão para pontos médios entre azul e vermelho quando o modo de entrada difere.
  - `Yellow Line`, `Blue Line`, `Red Line`: sair em toques diretos das bandas superiores/inferiores correspondentes.
- Os indicadores de bloqueio para o modo de escala são redefinidos automaticamente quando a vela fecha no lado oposto da banda média, recriando o comportamento do EA.

## Gestão de riscos
- Os parâmetros `Stop Loss` e `Take Profit` são expressos em pips e convertidos em distâncias de preço absolutas através de `Pip Value` quando `StartProtection` é inicializado.
- Stops e alvos são opcionais; deixe a distância em zero para desabilitar a proteção respectiva.
- O volume de negociação é definido por `Order Volume` e aplicado a cada ordem a mercado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Entry Mode` | Escolhe a zona de Bollinger que aciona as entradas. | Entre amarela e azul |
| `Closure Mode` | Define a banda ou ponto médio de tomada de lucro. | Entre azul e vermelha |
| `Bands Period` | Comprimento de período compartilhado por todas as Bollinger Bands. | 140 |
| `Deviation` | Multiplicador de desvio padrão para a banda amarela (azul é metade, vermelha é o dobro). | 2.0 |
| `Use RSI Filter` | Ativa a lógica de confirmação RSI. | false |
| `RSI Period` | Período de média do RSI. | 8 |
| `RSI Lower` | Limite de sobrecompra; sobrevenda usa `100 - valor`. | 70 |
| `Use Stochastic Filter` | Ativa a lógica de confirmação %K. | true |
| `Stochastic Period` | Período principal de retrocesso %K (suavização fixa em 3/3 SMA). | 20 |
| `Stochastic Lower` | Limite de sobrecompra; sobrevenda usa `100 - valor`. | 95 |
| `Bar Shift` | Número de barras finalizadas para olhar para trás em busca de valores de indicadores. | 1 |
| `Only One Position` | Se habilitado, abre novas operações apenas quando não há posição ativa. | true |
| `Order Volume` | Volume enviado com cada ordem a mercado. | 1 |
| `Pip Value` | Valor de preço absoluto de um pip para conversão de stop/alvo. | 0.0001 |
| `Stop Loss` | Distância de stop protetor em pips (0 desabilita). | 200 |
| `Take Profit` | Distância de alvo protetor em pips (0 desabilita). | 200 |
| `Candle Type` | Tipo de dados usado para cálculos (velas de 1 minuto por padrão). | Período de 1m |

## Notas
- A estratégia processa apenas velas concluídas, portanto `Bar Shift` deve permanecer ≥ 1 para evitar referenciar barras inacabadas.
- Os filtros RSI e Estocástico usam a linha %K; a linha %D é calculada mas não usada, refletindo a implementação original do EA.
- A conversão mantém comentários e nomes de sinais em inglês e segue as diretrizes da API de alto nível do StockSharp (pipeline de indicadores baseado em Bind, sem acesso manual a buffers).

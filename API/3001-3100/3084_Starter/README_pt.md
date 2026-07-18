# Estratégia de Início (Starter)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Início (Starter)** é uma conversão do consultor especialista MetaTrader 5 "Starter (barabashkakvn's edition)". O sistema
aguarda que o Índice de Canal de Commodity (CCI) ricocheteie do território extremo de sobrevenda ou sobrecompra e confirma o movimento
com a inclinação de uma média móvel de longo prazo. Quando o momentum concorda com o filtro de tendência, a estratégia abre uma única
posição de mercado cujo tamanho é determinado por uma porcentagem de risco configurável do portfólio. Stops de proteção e um mecanismo
de trailing opcional reproduzem as regras de gerenciamento de dinheiro do especialista original.

## Lógica de Negociação

- **Filtro de tendência** — uma média móvel (MA) configurável deve subir mais rápido que `MaDelta` para permitir negociações compradas
  e cair mais rápido que `MaDelta` para permitir negociações vendidas. A estratégia suporta os mesmos métodos de suavização que a
  versão MQL (simples, exponencial, suavizada, ponderada linearmente).
- **Confirmação de CCI** — o Índice de Canal de Commodity deve cruzar novamente acima de `-CciLevel` de baixo para cima para acionar
  entradas compradas e cruzar abaixo de `CciLevel` de cima para baixo para acionar vendidas. O indicador é avaliado apenas em velas
  fechadas, espelhando o processamento barra a barra do original.
- **Modelo de posição única** — o algoritmo mantém no máximo uma posição aberta. Novos sinais são ignorados até que a negociação
  atual seja fechada, correspondendo à lógica do MetaTrader que filtra por número mágico e símbolo.

### Regras de Entrada

1. Aguardar o fechamento de uma vela.
2. Calcular os valores mais recentes e anteriores da média móvel nos deslocamentos configurados.
3. Calcular as leituras atuais e anteriores do CCI.
4. **Ir comprado** quando:
   - A inclinação da média móvel supera `MaDelta` (MA atual menos MA anterior).
   - O valor atual de CCI é maior que o anterior.
   - O CCI cruza para cima por `-CciLevel` (o anterior abaixo do limiar, o atual acima).
5. **Ir vendido** quando:
   - A inclinação da média móvel está abaixo de `-MaDelta`.
   - O valor atual de CCI é menor que o anterior.
   - O CCI cruza para baixo por `CciLevel` (o anterior acima do limiar, o atual abaixo).

### Regras de Saída

- **Stop-loss inicial** — se `StopLossPips` é maior que zero, o preço de entrada executado é deslocado por `StopLossPips * PriceStep`
  para calcular um stop de proteção inicial.
- **Trailing stop** — quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos, o stop é avançado sempre que o preço
  melhora em pelo menos o passo configurado. Negociações compradas movem o stop para `Close - TrailingStop`, vendidas para
  `Close + TrailingStop`.
- **Saída manual** — se o preço toca o nível do stop dentro do intervalo da vela, a estratégia fecha a posição com uma ordem a
  mercado e redefine o estado de proteção.

## Gerenciamento de Risco

- **Dimensionamento de posição** — o volume base é `Portfolio.CurrentValue * MaximumRisk / price`. Quando o corretor ou back-end
  relata um valor de patrimônio inválido, a estratégia recorre à propriedade `Volume` manual (padrão 1).
- **Redução por sequência de perdas** — após duas ou mais negociações perdedoras consecutivas, o volume é reduzido por
  `volume * losses / DecreaseFactor`, imitando a regra original de `DecreaseFactor`. Qualquer negociação lucrativa redefine o
  contador de perdas.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `MaximumRisk` | `0.02` | Fração do patrimônio arriscado por negociação ao dimensionar a posição. |
| `DecreaseFactor` | `3` | Divisor de redução de lote aplicado após duas ou mais negociações perdedoras consecutivas. |
| `CciPeriod` | `14` | Número de barras usadas pelo Índice de Canal de Commodity. |
| `CciLevel` | `100` | Limiar de sobrevenda/sobrecompra para cruzamentos de CCI. |
| `CciCurrentBar` | `0` | Deslocamento do valor atual de CCI (0 = última vela). |
| `CciPreviousBar` | `1` | Deslocamento do valor anterior de CCI. |
| `MaPeriod` | `120` | Período do filtro de tendência da média móvel. |
| `MaMethod` | `Simple` | Método de suavização da média móvel (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaCurrentBar` | `0` | Deslocamento aplicado ao valor da média móvel. |
| `MaDelta` | `0.001` | Diferença de inclinação mínima entre leituras atuais e anteriores de MA. |
| `StopLossPips` | `0` | Distância do stop-loss inicial em pips (0 desativa o stop). |
| `TrailingStopPips` | `5` | Distância base do trailing stop em pips (0 desativa o trailing). |
| `TrailingStepPips` | `5` | Melhoria mínima em pips antes de avançar o trailing stop. |
| `CandleType` | Período `30m` | Assinatura de vela principal processada pela estratégia. |

## Notas de Implementação

- Os buffers do indicador são armazenados em cache internamente para que a estratégia possa acessar valores históricos com deslocamentos
  arbitrários, replicando a abordagem MQL de indexar arrays de indicadores.
- O tamanho do pip é derivado de `Security.PriceStep`. Se o instrumento não relatar um passo de preço válido, as distâncias de stop
  e trailing são tratadas como zero.
- Todos os comentários dentro do código são escritos em inglês de acordo com as diretrizes do repositório.
- A versão Python é intencionalmente omitida conforme solicitado.

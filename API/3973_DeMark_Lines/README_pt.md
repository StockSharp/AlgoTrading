# Estratégia de Linhas DeMark
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia DeMark Lines é uma conversão do indicador MetaTrader "DeMark_lines" (MQL/8296). O script original desenhou linhas de tendência DeMark com base em oscilações máximas e mínimas recentes e destacou rompimentos com alertas opcionais. Esta implementação StockSharp transforma a lógica de visualização em uma estratégia de breakout automatizada. Ele verifica continuamente as linhas de tendência de baixa e de alta formadas por pontos de pivô validados e abre posições quando a ação do preço quebra essas linhas de forma decisiva.

## Lógica de negociação
1. **Detecção de pivô** – as velas finalizadas são processadas em ordem cronológica. Uma vela se torna uma alta oscilante quando sua máxima é estritamente superior às velas *PivotDepth* anteriores e não inferior às velas *PivotDepth* seguintes. Os mínimos do balanço seguem a condição espelhada para os mínimos.
2. **Construção da linha de tendência** – as duas máximas de oscilação mais recentes formam a linha de resistência de tendência de baixa ativa. Os dois mínimos mais recentes formam a linha de suporte de tendência de alta. Pivôs adicionais serão ignorados se ocorrerem muito próximos da âncora anterior, evitando linhas instáveis.
3. **Filtros de ruptura** – a estratégia mede o valor teórico da linha de tendência para o índice de barras atual. Um rompimento exige que o preço de fechamento exceda a linha de resistência (ou caia abaixo do suporte) em pelo menos *BreakoutBuffer* pips antes que as negociações sejam executadas.
4. **Colocação de ordem** – quando aparece um rompimento de alta, qualquer exposição curta é fechada e uma posição longa do volume da estratégia configurada é aberta. A lógica de rompimento de baixa reflete esse comportamento. Cada linha pode acionar um novo sinal somente após um novo pivô redefini-la, evitando entradas repetidas enquanto o preço oscila em torno do nível.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `PivotDepth` | Número de velas de cada lado necessárias para confirmar um pivô alto/baixo. Controla o rigor da detecção de swing. | 2 |
| `MinBarsBetweenPivots` | Distância mínima, em barras, entre dois pivôs do mesmo tipo. Evita âncoras sobrepostas e mantém as linhas de tendência estáveis. | 5 |
| `BreakoutBuffer` | Distância extra (em pips) adicionada além da linha de tendência antes que um rompimento seja considerado válido. Filtra toques ruidosos. | 2 |
| `CandleType` | Tipo de dados de vela (período de tempo) usado para análise e geração de sinal. | Velas de 30 minutos |

## Notas de conversão
- Objetos visuais, alertas e notificações por e-mail do indicador original não são replicados. Em vez disso, as áreas do gráfico exibem séries de preços e as negociações da própria estratégia.
- A estratégia depende da assinatura de vela de alto nível de StockSharp API e usa buffers internos para validar pivôs sem fazer referência a métodos de histórico de indicadores proibidos pelas diretrizes.
- As negociações de breakout respeitam a propriedade base `Volume` e revertem automaticamente a exposição existente quando o breakout oposto é acionado.

## Dicas de uso
- Aumente `PivotDepth` em prazos mais altos para exigir oscilações mais amplas, o que reduz a frequência do sinal, mas melhora a confiabilidade da linha de tendência.
- Ajuste `BreakoutBuffer` para levar em conta a volatilidade do instrumento. Valores apertados favorecem entradas anteriores, enquanto buffers maiores ajudam a evitar falsificações.
- Combine a estratégia com gerenciamento de dinheiro externo ou módulos de proteção se o tratamento automatizado de saída (take-profit/stop-loss) for necessário, já que o script original focava apenas na detecção de fugas.

# Canal Comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Trade Channel é uma estratégia de reversão de canal convertida do consultor especialista MetaTrader "TradeChannel". O sistema desenha um canal de preço do máximo mais alto e do mínimo mais baixo em um número configurável de velas concluídas. Quando o canal para de se expandir e o preço testa novamente uma de suas fronteiras, a estratégia abre uma posição na direção oposta, esperando uma reversão para dentro do intervalo.

### Ideias centrais
- Use os indicadores **Mais alto** e **Mais baixo** para formar um canal semelhante a Donchian.
- Exija que o canal esteja plano (sem novos máximos ou mínimos) antes de abrir uma negociação.
- Apague o toque de resistência com posições curtas e o toque de suporte com posições longas.
- Coloque a parada de proteção inicial a um Average True Range (ATR) de distância do ponto de ruptura.
- Opcionalmente, siga o stop quando a negociação se mover a favor da posição.

## Parâmetros
| Nome | Descrição | Padrão | Otimização |
| --- | --- | --- | --- |
| `Volume` | Volume de negociação em lotes/contratos. | 1 | Ativado (0,1 → 2,0, etapa 0,1) |
| `ChannelLength` | Número de velas finalizadas usadas para calcular os limites do canal. | 20 | Ativado (10 → 60, etapa 5) |
| `AtrPeriod` | Período do indicador ATR para colocação de stop. | 4 | Ativado (2 → 20, etapa 2) |
| `TrailingPoints` | Compensação do trailing stop medida em etapas de preço do instrumento. Defina como `0` para desativar o rastreamento. | 30 | Ativado (0 → 100, etapa 10) |
| `CandleType` | Tipo de vela e prazo usado para cálculos. | Período de 30 minutos | - |

## Lógica de negociação
1. Assine a série de velas configurada e alimente três indicadores: `Highest`, `Lowest` e `ATR`.
2. Aguarde até que todos os indicadores estejam totalmente formados. Os primeiros valores concluídos inicializam o estado do canal e nenhuma negociação é realizada nessa vela.
3. Para cada nova vela acabada:
   - Atualize os limites do canal e calcule o pivô `(resistance + support + close) / 3`.
   - Verifique se a resistência (ou suporte) permanece inalterada em comparação com a vela anterior. Uma resistência plana permite configurações curtas, um suporte plano permite configurações longas.
   - **Entrada curta:** se a resistência for plana *e* a vela tocar a alta da resistência ou fechar entre o pivô e a resistência, envie uma ordem de venda de mercado.
   - **Entrada longa:** se o suporte estiver plano *e* a vela tocar a baixa do suporte ou fechar entre o suporte e o pivô, envie uma ordem de compra de mercado.
   - Apenas uma posição é permitida por vez. A estratégia aguarda o sinal do canal plano enquanto nenhuma negociação está aberta.
4. Na entrada:
   - Armazene o preço de entrada.
   - Defina o stop inicial como `resistance + ATR` para posições vendidas e `support − ATR` para posições compradas.
5. Gerenciar vagas abertas:
   - **Condições de saída para posições compradas:**
     - O preço toca o limite superior do canal enquanto permanece estável.
     - A mínima da vela cruza abaixo do nível de trailing/stop inicial.
   - **Condições de saída para shorts:**
     - O preço toca o limite inferior do canal enquanto permanece estável.
     - A alta da vela cruza acima do nível de trailing/stop inicial.
6. Parada móvel (se `TrailingPoints` > 0):
   - Converta a entrada em unidades de preço usando o `Security.Step` do instrumento (volta ao valor bruto se a etapa não estiver disponível).
   - Para posições compradas, quando o fechamento exceder o preço de entrada pelo deslocamento final, mova o stop para `close − offset`.
   - Para posições vendidas, quando o fechamento cair abaixo do preço de entrada pela compensação, mova o stop para `close + offset`.
   - O trailing stop nunca se move para trás; apenas aperta o nível de proteção.

## Notas
- Todas as decisões são tomadas em velas finalizadas para permanecerem alinhadas com a lógica MQL original que usava `High[1]`, `Low[1]` e `Close[1]`.
- A verificação de igualdade entre o limite do canal atual e o anterior é tolerante às etapas de preços dos instrumentos para evitar falhas de ponto flutuante.
- As paradas finais dependem de metadados `Security.Step` corretos. Se a exchange não fornecer isso, o valor do ponto bruto será usado.
- A estratégia não envia e-mails nem ajusta o dimensionamento da posição dinamicamente, pois esses recursos eram específicos da plataforma na implementação do MQL.

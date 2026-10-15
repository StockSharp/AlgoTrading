# Estratégia cruzada (MQL conversão 27596)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Cruzada** é uma conversão direta do MetaTrader consultor especialista `Cross.mq4` (entrada de repositório `MQL/27596`). O EA original negociou uma única média móvel exponencial (EMA) cruzada medida nos preços de abertura da barra e aplicou níveis de take-profit e stop loss de distância fixa. Esta porta StockSharp mantém a lógica de negociação intacta enquanto usa recursos API de alto nível, como assinaturas de velas, vinculação de indicadores e rastreamento de posição gerenciada.

## Lógica de negociação
1. **Indicador** – uma média móvel exponencial única (EMA) calculada a partir dos preços de fechamento das velas. O período é configurável e o padrão é 200, correspondendo à origem MQL.
2. **Detecção de sinal** – em cada vela finalizada, a estratégia compara a vela aberta com o valor EMA:
   - Um **sinal de alta** ocorre quando a vela abre acima de EMA depois de abrir anteriormente nele ou abaixo dele. Isso reproduz a chamada `Cross(0, Open[0] > EMA)` no script MQL.
   - Um **sinal de baixa** ocorre quando a vela abre abaixo do EMA depois de abrir anteriormente nele ou acima dele (`Cross(1, Open[0] < EMA)` no código original).
3. **Gerenciamento de Posição** – quando um sinal é acionado, a estratégia reverte totalmente a posição atual:
   - Se uma linha de alta aparecer enquanto estiver plana ou vendida, ela compra volume suficiente para cobrir a exposição curta e abrir uma nova posição longa.
   - Se uma linha de baixa aparecer enquanto estiver plana ou longa, ela vende volume suficiente para nivelar a exposição longa e estabelecer uma posição curta.
4. **Controle de risco** – após entrar em uma posição, a estratégia monitora os máximos e mínimos das velas para implementar saídas fixas de take-profit e stop loss em unidades de variação de preço. Essas saídas emulam as chamadas `OrderSend` que definem `TakeProfit` e `StopLoss` em MetaTrader.

## Parâmetros
| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `EMA Length` | 200 | Período do EMA usado para detecção cruzada. Deve ser maior que zero. |
| `Take Profit (steps)` | 200 | Distância até o nível de lucro medido em etapas de preço. Defina como zero para desativar a meta de lucro. |
| `Stop Loss (steps)` | 100 | Distância até o stop de proteção medida em etapas de preço. Defina como zero para desativar a parada. |
| `Candle Type` | Período de 1 minuto | Fonte de dados Candle processada pela estratégia. Você pode mudar para outros intervalos de tempo ou tipos de velas personalizados suportados por StockSharp. |

O volume negociado é controlado pela propriedade `Volume` da estratégia. Quando chega um sinal de reversão, a estratégia envia `Volume + |Position|` para garantir que a exposição existente seja fechada antes de abrir a nova posição.

## Fluxo de Execução
1. `OnStarted` assina a série de velas configurada e vincula o indicador EMA usando o auxiliar de alto nível `Bind`.
2. O manipulador pula velas inacabadas e espera até que EMA esteja totalmente formado. Depois de pronto, ele:
   - Gerencia a posição ativa verificando os níveis de stop loss e takeprofit em relação aos valores máximos/ mínimos da vela.
   - Detecta cruzamentos de alta e baixa com base no preço de abertura da vela em relação ao EMA.
   - Emite ordens de mercado para reverter a posição quando aparece um novo sinal.
3. `OnNewMyTrade` rastreia o preço médio de entrada e a direção da posição ativa para que as verificações de saída usem níveis precisos mesmo ao escalar para negociações.
4. Objetos gráficos opcionais são criados (se um gráfico estiver disponível) para exibir velas, a linha EMA e negociações executadas.

## Detalhes de gerenciamento de risco
- **Stop Loss** – calculado como `entry price ± stop steps × price step` dependendo da direção. A estratégia sai imediatamente quando a vela mínima (longa) ou máxima (curta) ultrapassa o nível de stop.
- **Take Profit** – calculado de forma semelhante usando as etapas de lucro configuradas. Atingir o alvo fecha toda a posição durante a vela onde a máxima/mínima ultrapassa o limite.
- **Proteção de conta** – `StartProtection()` é invocado uma vez na inicialização para que a estratégia respeite quaisquer regras de proteção global configuradas em ambientes StockSharp.

## Dicas de personalização
- Prazos mais curtos ou durações EMA criam reversões mais frequentes. Combine com distâncias de parada maiores para evitar serras elétricas.
- Para negociar vários símbolos, instancie instâncias de estratégia separadas com seus próprios títulos e tipos de velas.
- Ao otimizar, mantenha o comprimento EMA e as distâncias stop/take dentro de limites realistas para a volatilidade do instrumento e o tamanho do tick.

## Notas de conversão
- A matriz MQL `crossed[2]` é mapeada para dois sinalizadores booleanos internos que persistem entre velas.
- A função MQL `OrderSend` é representada pelos ajudantes `BuyMarket` e `SellMarket` de StockSharp, garantindo que tanto a reversão quanto as novas entradas espelhem o comportamento original.
- Os valores EMA são fornecidos por meio do retorno de chamada de ligação, evitando chamadas `GetValue` diretas conforme exigido pelas diretrizes do repositório.

Seguindo esses detalhes, você pode reproduzir a estratégia original do MetaTrader dentro do StockSharp enquanto mantém controle total sobre fontes de dados, otimização de parâmetros e gráficos.

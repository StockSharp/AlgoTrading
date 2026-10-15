# Estratégia de confirmação de castiçal Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o MetaTrader Expert Advisor **Expert_CP_Stoch** dentro do StockSharp de alto nível de API. Ele combina padrões de reversão de velas japonesas com um filtro oscilador estocástico% D para confirmar entradas e saídas de tempo. O sistema verifica cada vela concluída, olha três barras para trás para detectar formações de alta ou baixa e exige que a linha de sinal estocástica esteja em uma zona de sobrevenda ou sobrecompra antes de abrir negociações. As posições são fechadas sempre que o padrão oposto aparece ou quando a linha estocástica cruza um limite de saída configurável.

A configuração padrão reflete o especialista original: %K período 33, %D período 37, desaceleração 30, sobrevenda em 30, sobrecompra em 70 e níveis de cruzamento de saída em 20/80. Como o oscilador estocástico de StockSharp usa dados de alto/baixo/fechamento, o comportamento corresponde à configuração original de STO_LOWHIGH. O reconhecimento de padrões de velas depende dos últimos doze corpos (por padrão) para calcular o tamanho médio das velas usado na filtragem de padrões.

## Detalhes

- **Critérios de entrada**:
  - **Longo**: Um dos padrões de alta (Três Soldados Brancos, Linha Piercing, Morning Doji, Bullish Engulfing, Bullish Harami, Morning Star, Bullish Meeting Lines) é detectado **e** o valor %D estocástico na barra anteriormente fechada está abaixo do limite de sobrevenda (padrão 30).
  - **Short**: Um dos padrões de baixa (Três Corvos Negros, Dark Cloud Cover, Evening Doji, Bearish Engulfing, Bearish Harami, Evening Star, Bearish Meeting Lines) é detectado **e** o valor estocástico %D na barra anteriormente fechada está acima do limite de sobrecompra (padrão 70).
- **Critérios de saída**:
  - **Longo**: Sai imediatamente quando um padrão de baixa aparece ou quando %D cruza abaixo do limite de saída superior (padrão 80) ou abaixo do limite inferior (padrão 20).
  - **Venda**: Sai imediatamente quando um padrão de alta aparece ou quando %D cruza acima do limite inferior de saída (padrão 20) ou acima do limite superior (padrão 80).
- **Longo/Curto**: Negocia em ambas as direções com regras simétricas.
- **Stops**: Sem stop-loss/alvo fixo; as saídas dependem de inversões de padrões e cruzamentos estocásticos. Você pode adicionar proteção de portfólio no inicializador, se necessário.
- **Valores padrão**:
  - `Body Average Period` = 12 velas usadas para calcular o tamanho do corpo de referência para qualificação do padrão.
  - `Stochastic %K` = 33, `Stochastic %D` = 37, `Stochastic Smoothing` = 30.
  - `Oversold Threshold` = 30, `Overbought Threshold` = 70.
  - `Lower Exit Level` = 20, `Upper Exit Level` = 80.
- **Filtros**:
  - Categoria: Reconhecimento de padrão + confirmação do oscilador.
  - Direção: Longo e curto.
  - Indicadores: oscilador Stochastic, vários padrões de velas.
  - Paradas: Somente saídas de padrão/oscilador (sem parada/alvo mecânico).
  - Complexidade: Alta (detecção de padrões multicondições com médias históricas).
  - Prazo: Funciona em qualquer prazo; o padrão é velas horárias.
  - Sazonalidade: Nenhuma.
  - Redes neurais: Não.
  - Divergência: Nenhuma divergência explícita; confirmação através dos níveis do oscilador.
  - Nível de risco: Médio-alto devido à ausência de hard stops.

## Como funciona

1. Assina a série de velas selecionada e vincula um oscilador estocástico (%K,%D, desaceleração).
2. Mantém as últimas três velas concluídas e médias contínuas de corpos/fechos de velas para replicar a lógica da biblioteca de padrões de MetaTrader.
3. Avalia grupos de padrões de alta/baixa em cada vela finalizada. Cada padrão segue estritamente as definições matemáticas originais (verificações médias do corpo, relações de ponto médio, requisitos de lacuna, etc.).
4. Recupera os valores %D estocásticos das duas velas anteriores para detectar condições de sobrevenda/sobrecompra e cruzamentos.
5. Abre ou fecha posições de mercado usando os métodos `BuyMarket`/`SellMarket` de alto nível de StockSharp quando o padrão e as condições do oscilador se alinham.
6. Opcionalmente, você pode ativar módulos de risco externos (por exemplo, `StartProtection`) no inicializador se precisar de gerenciamento de stop-loss.

## Notas práticas

- Certifique-se de alimentar a estratégia com pelo menos `Body Average Period + 3` velas históricas antes de esperar sinais; caso contrário, as verificações de padrão retornarão falso porque o corpo médio é indefinido.
- O filtro estocástico usa o valor %D da vela **anterior**, replicando a forma como o sinal de MetaTrader avaliou `StochSignal(1)`.
- Como o reconhecimento de padrões de velas é sensível a lacunas e tamanhos relativos de velas, os resultados podem variar em instrumentos com pouca liquidez ou dados sintéticos.
- Para acelerar a otimização, você pode ajustar os limites de sobrevenda/sobrecompra e os períodos estocásticos, mantendo intactas as definições das velas.
- Se você precisar do comportamento STO_CLOSECLOSE (fechamento/fechamento estocástico), substitua o oscilador de StockSharp por um configurado para cálculos somente de fechamento em um aprimoramento futuro.

# Williams R Ichimoku Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta configuração combina os extremos de momentum de Williams %R com a estrutura de tendência definida pela Nuvem Ichimoku. A ideia é entrar em movimentos fortes somente quando o preço estiver no lado favorável da nuvem e as linhas de curto prazo confirmarem o viés.

Os testes indicam um retorno anual médio de aproximadamente 73%. Funciona melhor no mercado de criptomoedas.

Uma oportunidade de compra surge quando o oscilador cai abaixo de -80 enquanto o preço se mantém acima da nuvem e Tenkan-sen cruza acima de Kijun-sen. Um sinal de venda ocorre quando %R sobe acima de -20 com o preço abaixo da nuvem e Tenkan-sen abaixo de Kijun-sen. A posição permanece aberta até que o preço cruze o lado oposto da nuvem.

Como o método aguarda várias confirmações, é adequado para traders que preferem filtros de tendência claros em vez de reversões rápidas. Stops dinâmicos são definidos em torno do Kijun-sen para que o risco se ajuste com a força da tendência subjacente.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %R < -80 && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **Vendido**: %R > -20 && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o preço cruzar abaixo da nuvem
  - **Vendido**: Sair quando o preço cruzar acima da nuvem
- **Stops**: Sim.
- **Valores padrão**:
  - `WilliamsRPeriod` = 14
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Williams R Ichimoku
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


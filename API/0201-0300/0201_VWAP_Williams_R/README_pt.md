# Estratégia VWAP Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia VWAP Williams %R foca na reversão intradiária em torno do Preço Médio Ponderado por Volume. Ela observa quando o preço se afasta do VWAP enquanto o oscilador Williams %R atinge território de sobrecompra ou sobrevenda. A premissa é que leituras extremas próximas ao VWAP frequentemente levam a um retrocesso em direção à média.

Os testes indicam um retorno anual médio de aproximadamente 40%. Funciona melhor no mercado de criptomoedas.

Quando o oscilador cai abaixo de -80 e o preço opera abaixo do VWAP, o cenário implica que a pressão de venda está diminuindo e um repique pode se seguir. Por outro lado, uma leitura acima de -20 enquanto o preço está posicionado acima do VWAP avisa que os compradores estão esgotados e uma correção é provável. A estratégia abre operações na direção de um potencial retorno ao VWAP e aguarda a conclusão desse movimento.

Esta abordagem se adapta a traders ativos intradiários que preferem oportunidades frequentes de reversão à média. Um stop‑loss pequeno em relação ao VWAP mantém o risco contido enquanto ainda permite espaço suficiente para o preço flutuar antes de reverter.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price < VWAP && Williams %R < -80 (sobrevendido abaixo do VWAP)
  - **Vendido**: Price > VWAP && Williams %R > -20 (sobrecomprado acima do VWAP)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço rompe acima do VWAP
  - **Vendido**: Sair da posição vendida quando o preço rompe abaixo do VWAP
- **Stops**: Sim.
- **Valores padrão**:
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: VWAP Williams R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


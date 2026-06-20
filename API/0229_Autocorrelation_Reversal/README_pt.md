# Estratégia de Reversão por Autocorrelação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia analisa a autocorrelação de preços de curto prazo para avaliar se os movimentos recentes têm probabilidade de reverter. A autocorrelação negativa sugere que as mudanças de preço sucessivas tendem a alternar de direção, criando condições de reversão à média.

Os testes indicam um retorno anual médio de aproximadamente 124%. Funciona melhor no mercado de câmbio.

Quando a autocorrelação calculada cai abaixo do limiar e o preço está abaixo de uma média móvel, o sistema compra antecipando uma recuperação. Se a autocorrelação for negativa e o preço estiver acima da média, uma posição vendida é aberta. As saídas ocorrem quando o preço cruza a média ou a autocorrelação sobe acima do limiar.

A abordagem é adequada para traders que buscam vantagens estatísticas em vez de padrões gráficos. Um stop-loss percentual é aplicado para proteger contra tendências sustentadas que violem a reversão esperada.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Autocorrelation < Threshold && Close < MA
  - **Vendido**: Autocorrelation < Threshold && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Close > MA ou autocorrelation > Threshold
  - **Vendido**: Sair quando Close < MA ou autocorrelation > Threshold
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `AutoCorrPeriod` = 20
  - `AutoCorrThreshold` = -0.3m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: Autocorrelation, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


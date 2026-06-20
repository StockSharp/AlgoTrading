# Estratégia de Divergência de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Divergência de Momentum compara leituras de momentum com a direção do preço para detectar sinais precoces de uma reversão. Divergências ocorrem quando o preço faz um novo extremo mas o indicador de momentum não confirma, sugerindo um enfraquecimento da força.

Os testes indicam um retorno anual médio de aproximadamente 106%. Funciona melhor no mercado de ações.

Uma configuração de alta ocorre quando o preço registra um mínimo mais baixo enquanto o oscilador de momentum imprime um mínimo mais alto. Uma configuração de baixa se forma quando o preço empurra para um máximo mais alto mas o momentum não segue. As posições são fechadas quando o momentum cruza de volta por zero ou a divergência é invalidada.

Esta abordagem atrai traders que buscam antecipar pontos de inflexão em vez de seguir tendências. Os stops são usados para controlar o risco caso o mercado continue a se mover contra o sinal de divergência.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: O preço faz um mínimo mais baixo && O Momentum mostra um mínimo mais alto
  - **Vendido**: O preço faz um máximo mais alto && O Momentum mostra um máximo mais baixo
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o momentum cruza abaixo de zero
  - **Vendido**: Sair quando o momentum cruza acima de zero
- **Stops**: Sim, stop-loss fixo.
- **Valores padrão**:
  - `MomentumPeriod` = 14
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Momentum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

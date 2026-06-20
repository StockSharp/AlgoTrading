# Modelo Base de Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta pasta fornece um andaime mínimo para construir ideias de trading personalizadas.
A estratégia apenas calcula uma única média móvel exponencial e expõe uma ampla gama
de parâmetros comuns: habilitação de operações compradas ou vendidas, take profit e
stop loss opcionais, e intervalos de otimização. Os desenvolvedores podem inserir sua
própria lógica de entrada e saída nos espaços reservados para prototipar rapidamente
novos sistemas.

O modelo também demonstra como iniciar o módulo de proteção integrado com alvos
baseados em porcentagem, facilitando a experimentação com diferentes configurações de
risco. Como nenhum sinal real está incluído, este script não se destina a ser operado
como está, mas sim a servir como ponto de partida para pesquisas adicionais.

## Detalhes

- **Critérios de entrada**: Não implementados – substituir por regras personalizadas.
- **Comprado/Vendido**: Configurável via parâmetros.
- **Critérios de saída**: Não implementados – substituir por regras personalizadas.
- **Stops**: Take profit e stop loss percentuais opcionais gerenciados pelo módulo de proteção.
- **Valores padrão**:
  - Comprimento EMA = 10.
  - Take profit = 1.2%, Stop loss = 1.8% (desabilitado por padrão).
- **Filtros**:
  - Categoria: Modelo
  - Direção: Configurável
  - Indicadores: EMA
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Definido pelo usuário

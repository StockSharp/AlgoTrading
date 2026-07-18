# Estratégia de Exemplo para Strategy Tester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo ilustra como o momentum e a força da tendência podem ser combinados para
formar um sistema discrecional básico. Uma inclinação de regressão linear mede o
momentum de curto prazo enquanto o Average Directional Index avalia a persistência de
um movimento. Duas regras independentes acionam entradas: um pivô de momentum acompanhado
de uma queda no ADX, ou um novo máximo de ADX com momentum girando para cima a partir
de valores negativos.

A estratégia é intencionalmente simples e foca em posições compradas. Destina-se como
modelo para testar ideias como níveis de risco baseados em ATR e controles de saída
opcionais. Os desenvolvedores podem expandir a lógica de saída ou adicionar tratamento
de stop-loss para transformá-la em um modelo de trading completo.

## Detalhes

- **Critérios de entrada**:
  - Pivô alto de momentum e ADX em declínio.
  - Pivô alto de ADX com momentum subindo de valores negativos.
- **Comprado/Vendido**: Somente comprado por padrão.
- **Critérios de saída**:
  - Pivô alto de momentum (se a saída por momentum estiver habilitada).
  - Espaço reservado para saída de estratégia personalizada.
- **Stops**: Nenhum; os valores ATR estão disponíveis para uso externo.
- **Valores padrão**:
  - Comprimento de momentum = 20, comprimento DI = 14.
  - Nível-chave ADX = 25, comprimento ATR = 14.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Regressão linear, ADX, ATR
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto/médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (pivôs de momentum)
  - Nível de risco: Médio
